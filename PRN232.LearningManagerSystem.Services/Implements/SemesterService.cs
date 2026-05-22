using Microsoft.EntityFrameworkCore;
using PRN232.LearningManagerSystem.Repositories.Entities;
using PRN232.LearningManagerSystem.Repositories.Interfaces;
using PRN232.LearningManagerSystem.Services.Helpers;
using PRN232.LearningManagerSystem.Services.Interfaces;
using PRN232.LearningManagerSystem.Services.Models.BusinessModels;
using PRN232.LearningManagerSystem.Services.Models.Common;

namespace PRN232.LearningManagerSystem.Services.Implements;

public class SemesterService : ISemesterService
{
    private readonly ISemesterRepository _semesterRepository;

    public SemesterService(ISemesterRepository semesterRepository)
    {
        _semesterRepository = semesterRepository;
    }

    public async Task<ServicePagedResult<object>> GetSemestersAsync(ServiceListQueryParameters query)
    {
        try
        {
            var queryable = await _semesterRepository.GetQueryableAsync();

            // Search
            if (!string.IsNullOrWhiteSpace(query.Search))
            {
                var search = query.Search.ToLower();
                queryable = queryable.Where(s => s.SemesterName.ToLower().Contains(search));
            }

            // Expand
            var expandList = (query.Expand ?? "").Split(',', StringSplitOptions.RemoveEmptyEntries)
                             .Select(e => e.Trim().ToLower()).ToList();
            bool expandCourses = expandList.Contains("courses");

            if (expandCourses)
            {
                queryable = queryable
                    .Include(s => s.Courses)
                    .ThenInclude(c => c.Subject);
            }

            // Sort
            if (!string.IsNullOrWhiteSpace(query.Sort))
                queryable = ApplySorting(queryable, query.Sort);

            var totalItems = await queryable.CountAsync();

            var items = await queryable
                .Skip((query.Page - 1) * query.Size)
                .Take(query.Size)
                .ToListAsync();

            var businessModels = items
                .Select(s => MapEntityToBusiness(s, includeCourses: expandCourses))
                .ToList();

            var pagination = new ServicePaginationMetadata
            {
                Page       = query.Page,
                PageSize   = query.Size,
                TotalItems = totalItems,
                TotalPages = (int)Math.Ceiling((double)totalItems / query.Size)
            };

            if (!string.IsNullOrWhiteSpace(query.Fields))
            {
                var selected = businessModels.Select(r => FieldSelector.SelectFields(r, query.Fields)).ToList();
                return ServicePagedResult<object>.Ok((object)selected, pagination);
            }

            return ServicePagedResult<object>.Ok((object)businessModels, pagination);
        }
        catch (Exception ex)
        {
            return ServicePagedResult<object>.ServerError("An unexpected error occurred.", ex.Message);
        }
    }

    public async Task<ServiceResult<SemesterBusinessModel>> GetSemesterByIdAsync(int id)
    {
        try
        {
            var semester = await _semesterRepository.GetByIdAsync(id);
            if (semester == null)
                return ServiceResult<SemesterBusinessModel>.NotFound($"Semester with ID {id} not found.");

            return ServiceResult<SemesterBusinessModel>.Ok(MapEntityToBusiness(semester, includeCourses: true));
        }
        catch (Exception ex)
        {
            return ServiceResult<SemesterBusinessModel>.ServerError("An unexpected error occurred.", ex.Message);
        }
    }

    public async Task<ServiceResult<SemesterBusinessModel>> CreateSemesterAsync(SemesterCreateBusinessModel model)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(model.SemesterName))
                return ServiceResult<SemesterBusinessModel>.BadRequest("SemesterName is required.");

            if (model.EndDate <= model.StartDate)
                return ServiceResult<SemesterBusinessModel>.BadRequest("EndDate must be greater than StartDate.");

            var semester = new Semester
            {
                SemesterName = model.SemesterName,
                StartDate    = model.StartDate,
                EndDate      = model.EndDate
            };

            await _semesterRepository.AddAsync(semester);
            await _semesterRepository.SaveChangesAsync();

            var created = await _semesterRepository.GetByIdAsync(semester.SemesterId);
            return ServiceResult<SemesterBusinessModel>.Created(
                MapEntityToBusiness(created!, includeCourses: true), "Semester created successfully.");
        }
        catch (Exception ex)
        {
            return ServiceResult<SemesterBusinessModel>.ServerError("An unexpected error occurred.", ex.Message);
        }
    }

    public async Task<ServiceResult<SemesterBusinessModel>> UpdateSemesterAsync(int id, SemesterUpdateBusinessModel model)
    {
        try
        {
            var semester = await _semesterRepository.GetByIdAsync(id);
            if (semester == null)
                return ServiceResult<SemesterBusinessModel>.NotFound($"Semester with ID {id} not found.");

            if (string.IsNullOrWhiteSpace(model.SemesterName))
                return ServiceResult<SemesterBusinessModel>.BadRequest("SemesterName is required.");

            if (model.EndDate <= model.StartDate)
                return ServiceResult<SemesterBusinessModel>.BadRequest("EndDate must be greater than StartDate.");

            semester.SemesterName = model.SemesterName;
            semester.StartDate    = model.StartDate;
            semester.EndDate      = model.EndDate;

            _semesterRepository.Update(semester);
            await _semesterRepository.SaveChangesAsync();

            var updated = await _semesterRepository.GetByIdAsync(id);
            return ServiceResult<SemesterBusinessModel>.Ok(
                MapEntityToBusiness(updated!, includeCourses: true), "Semester updated successfully.");
        }
        catch (Exception ex)
        {
            return ServiceResult<SemesterBusinessModel>.ServerError("An unexpected error occurred.", ex.Message);
        }
    }

    public async Task<ServiceResult<bool>> DeleteSemesterAsync(int id)
    {
        try
        {
            var semester = await _semesterRepository.GetByIdAsync(id);
            if (semester == null)
                return ServiceResult<bool>.NotFound($"Semester with ID {id} not found.");

            _semesterRepository.Delete(semester);
            await _semesterRepository.SaveChangesAsync();
            return ServiceResult<bool>.Ok(true, "Semester deleted successfully.");
        }
        catch (Exception ex) when (ex.InnerException?.Message.Contains("REFERENCE") == true
                                || ex.Message.Contains("DELETE"))
        {
            return ServiceResult<bool>.BadRequest("Cannot delete semester because it has associated courses.");
        }
        catch (Exception ex)
        {
            return ServiceResult<bool>.ServerError("An unexpected error occurred.", ex.Message);
        }
    }

    // ---- Mapping: Entity → BusinessModel ----

    private static SemesterBusinessModel MapEntityToBusiness(Semester s, bool includeCourses = false)
    {
        return new SemesterBusinessModel
        {
            SemesterId   = s.SemesterId,
            SemesterName = s.SemesterName,
            StartDate    = s.StartDate,
            EndDate      = s.EndDate,
            CourseCount  = s.Courses?.Count ?? 0,
            Courses      = includeCourses
                ? s.Courses?.Select(c => new CourseBusinessModel
                {
                    CourseId    = c.CourseId,
                    CourseName  = c.CourseName ?? string.Empty,
                    SemesterId  = c.SemesterId,
                    SubjectId   = c.SubjectId,
                    SubjectCode = c.Subject?.SubjectCode ?? string.Empty,
                    SubjectName = c.Subject?.SubjectName ?? string.Empty,
                    Credit      = c.Subject?.Credit ?? 0,
                    DisplayName = $"{c.Subject?.SubjectCode ?? string.Empty} - {c.CourseName}"
                }).ToList()
                : null
        };
    }

    // ---- Sorting ----

    private static IQueryable<Semester> ApplySorting(IQueryable<Semester> queryable, string sort)
    {
        var parts = sort.Split(',', StringSplitOptions.RemoveEmptyEntries);
        IOrderedQueryable<Semester>? ordered = null;

        foreach (var part in parts)
        {
            var trimmed    = part.Trim();
            var descending = trimmed.StartsWith("-");
            var field      = trimmed.TrimStart('-').ToLower();

            IOrderedQueryable<Semester> next = (ordered == null)
                ? field switch
                {
                    "semesterid"   => descending ? queryable.OrderByDescending(s => s.SemesterId)   : queryable.OrderBy(s => s.SemesterId),
                    "semestername" => descending ? queryable.OrderByDescending(s => s.SemesterName) : queryable.OrderBy(s => s.SemesterName),
                    "startdate"    => descending ? queryable.OrderByDescending(s => s.StartDate)    : queryable.OrderBy(s => s.StartDate),
                    "enddate"      => descending ? queryable.OrderByDescending(s => s.EndDate)      : queryable.OrderBy(s => s.EndDate),
                    _              => queryable.OrderBy(s => s.SemesterId)
                }
                : field switch
                {
                    "semesterid"   => descending ? ordered.ThenByDescending(s => s.SemesterId)   : ordered.ThenBy(s => s.SemesterId),
                    "semestername" => descending ? ordered.ThenByDescending(s => s.SemesterName) : ordered.ThenBy(s => s.SemesterName),
                    "startdate"    => descending ? ordered.ThenByDescending(s => s.StartDate)    : ordered.ThenBy(s => s.StartDate),
                    "enddate"      => descending ? ordered.ThenByDescending(s => s.EndDate)      : ordered.ThenBy(s => s.EndDate),
                    _              => ordered.ThenBy(s => s.SemesterId)
                };

            ordered = next;
        }

        return ordered ?? queryable;
    }
}
