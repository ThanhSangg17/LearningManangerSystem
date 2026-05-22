using Microsoft.EntityFrameworkCore;
using PRN232.LearningManagerSystem.Repositories.Entities;
using PRN232.LearningManagerSystem.Repositories.Interfaces;
using PRN232.LearningManagerSystem.Services.Helpers;
using PRN232.LearningManagerSystem.Services.Interfaces;
using PRN232.LearningManagerSystem.Services.Models.BusinessModels;
using PRN232.LearningManagerSystem.Services.Models.Common;

namespace PRN232.LearningManagerSystem.Services.Implements;

public class StudentService : IStudentService
{
    private readonly IStudentRepository _studentRepository;

    public StudentService(IStudentRepository studentRepository)
    {
        _studentRepository = studentRepository;
    }

    public async Task<ServicePagedResult<object>> GetStudentsAsync(ServiceListQueryParameters query)
    {
        try
        {
            var queryable = await _studentRepository.GetQueryableAsync();

            // Search
            if (!string.IsNullOrWhiteSpace(query.Search))
            {
                var search = query.Search.ToLower();
                queryable = queryable.Where(s =>
                    s.FullName.ToLower().Contains(search) ||
                    s.Email.ToLower().Contains(search));
            }

            // Expand
            var expandList = (query.Expand ?? "").Split(',', StringSplitOptions.RemoveEmptyEntries)
                             .Select(e => e.Trim().ToLower()).ToList();

            bool expandEnrollments      = expandList.Contains("enrollments") || expandList.Contains("enrollments.course");
            bool expandEnrollmentCourse = expandList.Contains("enrollments.course");

            if (expandEnrollments)
            {
                queryable = queryable
                    .Include(s => s.Enrollments)
                    .ThenInclude(e => e.Course)
                        .ThenInclude(c => c.Semester);
                queryable = queryable
                    .Include(s => s.Enrollments)
                    .ThenInclude(e => e.Course)
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
                .Select(s => MapEntityToBusiness(s, includeEnrollments: expandEnrollments))
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

    public async Task<ServiceResult<StudentBusinessModel>> GetStudentByIdAsync(int id)
    {
        try
        {
            var student = await _studentRepository.GetByIdAsync(id);
            if (student == null)
                return ServiceResult<StudentBusinessModel>.NotFound($"Student with ID {id} not found.");

            return ServiceResult<StudentBusinessModel>.Ok(MapEntityToBusiness(student, includeEnrollments: true));
        }
        catch (Exception ex)
        {
            return ServiceResult<StudentBusinessModel>.ServerError("An unexpected error occurred.", ex.Message);
        }
    }

    public async Task<ServiceResult<StudentBusinessModel>> CreateStudentAsync(StudentCreateBusinessModel model)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(model.FullName))
                return ServiceResult<StudentBusinessModel>.BadRequest("FullName is required.");

            if (string.IsNullOrWhiteSpace(model.Email))
                return ServiceResult<StudentBusinessModel>.BadRequest("Email is required.");

            if (model.DateOfBirth >= DateTime.Now)
                return ServiceResult<StudentBusinessModel>.BadRequest("DateOfBirth must be in the past.");

            var existing = await _studentRepository.GetByEmailAsync(model.Email);
            if (existing != null)
                return ServiceResult<StudentBusinessModel>.BadRequest($"Email '{model.Email}' is already in use.");

            var student = new Student
            {
                FullName    = model.FullName,
                Email       = model.Email,
                DateOfBirth = model.DateOfBirth
            };

            await _studentRepository.AddAsync(student);
            await _studentRepository.SaveChangesAsync();

            var created = await _studentRepository.GetByIdAsync(student.StudentId);
            return ServiceResult<StudentBusinessModel>.Created(MapEntityToBusiness(created!, includeEnrollments: true), "Student created successfully.");
        }
        catch (Exception ex)
        {
            return ServiceResult<StudentBusinessModel>.ServerError("An unexpected error occurred.", ex.Message);
        }
    }

    public async Task<ServiceResult<StudentBusinessModel>> UpdateStudentAsync(int id, StudentUpdateBusinessModel model)
    {
        try
        {
            var student = await _studentRepository.GetByIdAsync(id);
            if (student == null)
                return ServiceResult<StudentBusinessModel>.NotFound($"Student with ID {id} not found.");

            if (string.IsNullOrWhiteSpace(model.FullName))
                return ServiceResult<StudentBusinessModel>.BadRequest("FullName is required.");

            if (string.IsNullOrWhiteSpace(model.Email))
                return ServiceResult<StudentBusinessModel>.BadRequest("Email is required.");

            if (model.DateOfBirth >= DateTime.Now)
                return ServiceResult<StudentBusinessModel>.BadRequest("DateOfBirth must be in the past.");

            var existingEmail = await _studentRepository.GetByEmailAsync(model.Email);
            if (existingEmail != null && existingEmail.StudentId != id)
                return ServiceResult<StudentBusinessModel>.BadRequest($"Email '{model.Email}' is already in use by another student.");

            student.FullName    = model.FullName;
            student.Email       = model.Email;
            student.DateOfBirth = model.DateOfBirth;

            _studentRepository.Update(student);
            await _studentRepository.SaveChangesAsync();

            var updated = await _studentRepository.GetByIdAsync(id);
            return ServiceResult<StudentBusinessModel>.Ok(MapEntityToBusiness(updated!, includeEnrollments: true), "Student updated successfully.");
        }
        catch (Exception ex)
        {
            return ServiceResult<StudentBusinessModel>.ServerError("An unexpected error occurred.", ex.Message);
        }
    }

    public async Task<ServiceResult<bool>> DeleteStudentAsync(int id)
    {
        try
        {
            var student = await _studentRepository.GetByIdAsync(id);
            if (student == null)
                return ServiceResult<bool>.NotFound($"Student with ID {id} not found.");

            _studentRepository.Delete(student);
            await _studentRepository.SaveChangesAsync();
            return ServiceResult<bool>.Ok(true, "Student deleted successfully.");
        }
        catch (Exception ex) when (ex.InnerException?.Message.Contains("REFERENCE") == true
                                || ex.Message.Contains("DELETE"))
        {
            return ServiceResult<bool>.BadRequest("Cannot delete student because they have associated enrollments.");
        }
        catch (Exception ex)
        {
            return ServiceResult<bool>.ServerError("An unexpected error occurred.", ex.Message);
        }
    }

    // ---- Mapping: Entity → BusinessModel ----

    private static StudentBusinessModel MapEntityToBusiness(Student s, bool includeEnrollments = false)
    {
        return new StudentBusinessModel
        {
            StudentId       = s.StudentId,
            FullName        = s.FullName,
            Email           = s.Email,
            DateOfBirth     = s.DateOfBirth,
            EnrollmentCount = s.Enrollments?.Count ?? 0,
            Enrollments     = includeEnrollments
                ? s.Enrollments?.Select(e => new EnrollmentBusinessModel
                {
                    EnrollmentId = e.EnrollmentId,
                    StudentId    = e.StudentId,
                    CourseId     = e.CourseId,
                    EnrollDate   = e.EnrollDate,
                    Status       = e.Status,
                    StudentName  = s.FullName,
                    StudentEmail = s.Email,
                    CourseName   = e.Course?.CourseName ?? string.Empty,
                    SubjectCode  = e.Course?.Subject?.SubjectCode   ?? string.Empty,
                    SemesterName = e.Course?.Semester?.SemesterName ?? string.Empty
                }).ToList()
                : null
        };
    }

    // ---- Sorting ----

    private static IQueryable<Student> ApplySorting(IQueryable<Student> queryable, string sort)
    {
        var parts = sort.Split(',', StringSplitOptions.RemoveEmptyEntries);
        IOrderedQueryable<Student>? ordered = null;

        foreach (var part in parts)
        {
            var trimmed    = part.Trim();
            var descending = trimmed.StartsWith("-");
            var field      = trimmed.TrimStart('-').ToLower();

            IOrderedQueryable<Student> next = (ordered == null)
                ? field switch
                {
                    "studentid"   => descending ? queryable.OrderByDescending(s => s.StudentId)   : queryable.OrderBy(s => s.StudentId),
                    "fullname"    => descending ? queryable.OrderByDescending(s => s.FullName)     : queryable.OrderBy(s => s.FullName),
                    "email"       => descending ? queryable.OrderByDescending(s => s.Email)        : queryable.OrderBy(s => s.Email),
                    "dateofbirth" => descending ? queryable.OrderByDescending(s => s.DateOfBirth)  : queryable.OrderBy(s => s.DateOfBirth),
                    _             => queryable.OrderBy(s => s.StudentId)
                }
                : field switch
                {
                    "studentid"   => descending ? ordered.ThenByDescending(s => s.StudentId)   : ordered.ThenBy(s => s.StudentId),
                    "fullname"    => descending ? ordered.ThenByDescending(s => s.FullName)     : ordered.ThenBy(s => s.FullName),
                    "email"       => descending ? ordered.ThenByDescending(s => s.Email)        : ordered.ThenBy(s => s.Email),
                    "dateofbirth" => descending ? ordered.ThenByDescending(s => s.DateOfBirth)  : ordered.ThenBy(s => s.DateOfBirth),
                    _             => ordered.ThenBy(s => s.StudentId)
                };

            ordered = next;
        }

        return ordered ?? queryable;
    }
}
