using Microsoft.EntityFrameworkCore;
using PRN232.LearningManagerSystem.Repositories.Entities;
using PRN232.LearningManagerSystem.Repositories.Interfaces;
using PRN232.LearningManagerSystem.Services.Helpers;
using PRN232.LearningManagerSystem.Services.Interfaces;
using PRN232.LearningManagerSystem.Services.Models.BusinessModels;
using PRN232.LearningManagerSystem.Services.Models.Common;

namespace PRN232.LearningManagerSystem.Services.Implements;

public class SubjectService : ISubjectService
{
    private readonly ISubjectRepository _subjectRepository;

    public SubjectService(ISubjectRepository subjectRepository)
    {
        _subjectRepository = subjectRepository;
    }

    public async Task<ServicePagedResult<object>> GetSubjectsAsync(ServiceListQueryParameters query)
    {
        try
        {
            var queryable = await _subjectRepository.GetQueryableAsync();

            // Search
            if (!string.IsNullOrWhiteSpace(query.Search))
            {
                var search = query.Search.ToLower();
                queryable = queryable.Where(s =>
                    s.SubjectCode.ToLower().Contains(search) ||
                    s.SubjectName.ToLower().Contains(search));
            }

            // Expand
            var expandList = (query.Expand ?? "").Split(',', StringSplitOptions.RemoveEmptyEntries)
                             .Select(e => e.Trim().ToLower()).ToList();
            bool expandCourses = expandList.Contains("courses");

            if (expandCourses)
            {
                queryable = queryable
                    .Include(s => s.Courses)
                    .ThenInclude(c => c.Semester);
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

    public async Task<ServiceResult<SubjectBusinessModel>> GetSubjectByIdAsync(int id)
    {
        try
        {
            var subject = await _subjectRepository.GetByIdAsync(id);
            if (subject == null)
                return ServiceResult<SubjectBusinessModel>.NotFound($"Subject with ID {id} not found.");

            return ServiceResult<SubjectBusinessModel>.Ok(MapEntityToBusiness(subject, includeCourses: true));
        }
        catch (Exception ex)
        {
            return ServiceResult<SubjectBusinessModel>.ServerError("An unexpected error occurred.", ex.Message);
        }
    }

    public async Task<ServiceResult<SubjectBusinessModel>> CreateSubjectAsync(SubjectCreateBusinessModel model)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(model.SubjectCode))
                return ServiceResult<SubjectBusinessModel>.BadRequest("SubjectCode is required.");

            if (string.IsNullOrWhiteSpace(model.SubjectName))
                return ServiceResult<SubjectBusinessModel>.BadRequest("SubjectName is required.");

            if (model.Credit <= 0)
                return ServiceResult<SubjectBusinessModel>.BadRequest("Credit must be greater than 0.");

            var existing = await _subjectRepository.GetByCodeAsync(model.SubjectCode);
            if (existing != null)
                return ServiceResult<SubjectBusinessModel>.BadRequest($"SubjectCode '{model.SubjectCode}' already exists.");

            var subject = new Subject
            {
                SubjectCode = model.SubjectCode,
                SubjectName = model.SubjectName,
                Credit      = model.Credit
            };

            await _subjectRepository.AddAsync(subject);
            await _subjectRepository.SaveChangesAsync();

            var created = await _subjectRepository.GetByIdAsync(subject.SubjectId);
            return ServiceResult<SubjectBusinessModel>.Created(
                MapEntityToBusiness(created!, includeCourses: true), "Subject created successfully.");
        }
        catch (Exception ex)
        {
            return ServiceResult<SubjectBusinessModel>.ServerError("An unexpected error occurred.", ex.Message);
        }
    }

    public async Task<ServiceResult<SubjectBusinessModel>> UpdateSubjectAsync(int id, SubjectUpdateBusinessModel model)
    {
        try
        {
            var subject = await _subjectRepository.GetByIdAsync(id);
            if (subject == null)
                return ServiceResult<SubjectBusinessModel>.NotFound($"Subject with ID {id} not found.");

            if (string.IsNullOrWhiteSpace(model.SubjectCode))
                return ServiceResult<SubjectBusinessModel>.BadRequest("SubjectCode is required.");

            if (string.IsNullOrWhiteSpace(model.SubjectName))
                return ServiceResult<SubjectBusinessModel>.BadRequest("SubjectName is required.");

            if (model.Credit <= 0)
                return ServiceResult<SubjectBusinessModel>.BadRequest("Credit must be greater than 0.");

            var existingCode = await _subjectRepository.GetByCodeAsync(model.SubjectCode);
            if (existingCode != null && existingCode.SubjectId != id)
                return ServiceResult<SubjectBusinessModel>.BadRequest(
                    $"SubjectCode '{model.SubjectCode}' is already used by another subject.");

            subject.SubjectCode = model.SubjectCode;
            subject.SubjectName = model.SubjectName;
            subject.Credit      = model.Credit;

            _subjectRepository.Update(subject);
            await _subjectRepository.SaveChangesAsync();

            var updated = await _subjectRepository.GetByIdAsync(id);
            return ServiceResult<SubjectBusinessModel>.Ok(
                MapEntityToBusiness(updated!, includeCourses: true), "Subject updated successfully.");
        }
        catch (Exception ex)
        {
            return ServiceResult<SubjectBusinessModel>.ServerError("An unexpected error occurred.", ex.Message);
        }
    }

    public async Task<ServiceResult<bool>> DeleteSubjectAsync(int id)
    {
        try
        {
            var subject = await _subjectRepository.GetByIdAsync(id);
            if (subject == null)
                return ServiceResult<bool>.NotFound($"Subject with ID {id} not found.");

            _subjectRepository.Delete(subject);
            await _subjectRepository.SaveChangesAsync();
            return ServiceResult<bool>.Ok(true, "Subject deleted successfully.");
        }
        catch (Exception ex) when (ex.InnerException?.Message.Contains("REFERENCE") == true
                                || ex.Message.Contains("DELETE"))
        {
            return ServiceResult<bool>.BadRequest("Cannot delete subject because it has associated courses.");
        }
        catch (Exception ex)
        {
            return ServiceResult<bool>.ServerError("An unexpected error occurred.", ex.Message);
        }
    }

    // ---- Mapping: Entity → BusinessModel ----

    private static SubjectBusinessModel MapEntityToBusiness(Subject s, bool includeCourses = false)
    {
        return new SubjectBusinessModel
        {
            SubjectId   = s.SubjectId,
            SubjectCode = s.SubjectCode,
            SubjectName = s.SubjectName,
            Credit      = s.Credit,
            CourseCount = s.Courses?.Count ?? 0,
            Courses     = includeCourses
                ? s.Courses?.Select(c => new CourseBusinessModel
                {
                    CourseId     = c.CourseId,
                    CourseName   = c.CourseName ?? string.Empty,
                    SemesterId   = c.SemesterId,
                    SubjectId    = c.SubjectId,
                    SemesterName = c.Semester?.SemesterName ?? string.Empty,
                    DisplayName  = $"{s.SubjectCode} - {c.CourseName}"
                }).ToList()
                : null
        };
    }

    // ---- Sorting ----

    private static IQueryable<Subject> ApplySorting(IQueryable<Subject> queryable, string sort)
    {
        var parts = sort.Split(',', StringSplitOptions.RemoveEmptyEntries);
        IOrderedQueryable<Subject>? ordered = null;

        foreach (var part in parts)
        {
            var trimmed    = part.Trim();
            var descending = trimmed.StartsWith("-");
            var field      = trimmed.TrimStart('-').ToLower();

            IOrderedQueryable<Subject> next = (ordered == null)
                ? field switch
                {
                    "subjectid"   => descending ? queryable.OrderByDescending(s => s.SubjectId)   : queryable.OrderBy(s => s.SubjectId),
                    "subjectcode" => descending ? queryable.OrderByDescending(s => s.SubjectCode) : queryable.OrderBy(s => s.SubjectCode),
                    "subjectname" => descending ? queryable.OrderByDescending(s => s.SubjectName) : queryable.OrderBy(s => s.SubjectName),
                    "credit"      => descending ? queryable.OrderByDescending(s => s.Credit)      : queryable.OrderBy(s => s.Credit),
                    _             => queryable.OrderBy(s => s.SubjectId)
                }
                : field switch
                {
                    "subjectid"   => descending ? ordered.ThenByDescending(s => s.SubjectId)   : ordered.ThenBy(s => s.SubjectId),
                    "subjectcode" => descending ? ordered.ThenByDescending(s => s.SubjectCode) : ordered.ThenBy(s => s.SubjectCode),
                    "subjectname" => descending ? ordered.ThenByDescending(s => s.SubjectName) : ordered.ThenBy(s => s.SubjectName),
                    "credit"      => descending ? ordered.ThenByDescending(s => s.Credit)      : ordered.ThenBy(s => s.Credit),
                    _             => ordered.ThenBy(s => s.SubjectId)
                };

            ordered = next;
        }

        return ordered ?? queryable;
    }
}
