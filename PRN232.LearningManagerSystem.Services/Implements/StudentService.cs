using Microsoft.EntityFrameworkCore;
using PRN232.LearningManagerSystem.Repositories.Entities;
using PRN232.LearningManagerSystem.Repositories.Interfaces;
using PRN232.LearningManagerSystem.Services.Helpers;
using PRN232.LearningManagerSystem.Services.Interfaces;
using PRN232.LearningManagerSystem.Services.Models.Common;
using PRN232.LearningManagerSystem.Services.Models.Requests;
using PRN232.LearningManagerSystem.Services.Models.Responses;

namespace PRN232.LearningManagerSystem.Services.Implements;

public class StudentService : IStudentService
{
    private readonly IStudentRepository _studentRepository;

    public StudentService(IStudentRepository studentRepository)
    {
        _studentRepository = studentRepository;
    }

    public async Task<PagedResponse<object>> GetStudentsAsync(ListQueryParameters query)
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

        // Expand for list
        var expandList = (query.Expand ?? "").Split(',', StringSplitOptions.RemoveEmptyEntries)
                         .Select(e => e.Trim().ToLower()).ToList();

        bool expandEnrollments = expandList.Contains("enrollments") || expandList.Contains("enrollments.course");
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
        {
            queryable = ApplySorting(queryable, query.Sort);
        }

        // Count total
        var totalItems = await queryable.CountAsync();

        // Paging
        var items = await queryable
            .Skip((query.Page - 1) * query.Size)
            .Take(query.Size)
            .ToListAsync();

        var responses = items.Select(s => MapToStudentResponse(s, expandEnrollments)).ToList();

        var pagination = new PaginationMetadata
        {
            Page = query.Page,
            PageSize = query.Size,
            TotalItems = totalItems,
            TotalPages = (int)Math.Ceiling((double)totalItems / query.Size)
        };

        // Field selection
        if (!string.IsNullOrWhiteSpace(query.Fields))
        {
            var selected = responses.Select(r => FieldSelector.SelectFields(r, query.Fields)).ToList();
            return PagedResponse<object>.SuccessResponse((object)selected, pagination);
        }

        return PagedResponse<object>.SuccessResponse((object)responses, pagination);
    }

    public async Task<ApiResponse<StudentResponse>> GetStudentByIdAsync(int id)
    {
        var student = await _studentRepository.GetByIdAsync(id);
        if (student == null)
            return ApiResponse<StudentResponse>.ErrorResponse($"Student with ID {id} not found.");

        var response = MapToStudentResponse(student, includeEnrollments: true);
        return ApiResponse<StudentResponse>.SuccessResponse(response);
    }

    public async Task<ApiResponse<StudentResponse>> CreateStudentAsync(CreateStudentRequest request)
    {
        // Validation
        if (string.IsNullOrWhiteSpace(request.FullName))
            return ApiResponse<StudentResponse>.ErrorResponse("FullName is required.");

        if (string.IsNullOrWhiteSpace(request.Email))
            return ApiResponse<StudentResponse>.ErrorResponse("Email is required.");

        if (request.DateOfBirth >= DateTime.Now)
            return ApiResponse<StudentResponse>.ErrorResponse("DateOfBirth must be in the past.");

        var existing = await _studentRepository.GetByEmailAsync(request.Email);
        if (existing != null)
            return ApiResponse<StudentResponse>.ErrorResponse($"Email '{request.Email}' is already in use.");

        var student = new Student
        {
            FullName = request.FullName,
            Email = request.Email,
            DateOfBirth = request.DateOfBirth
        };

        await _studentRepository.AddAsync(student);
        await _studentRepository.SaveChangesAsync();

        var created = await _studentRepository.GetByIdAsync(student.StudentId);
        return ApiResponse<StudentResponse>.SuccessResponse(
            MapToStudentResponse(created!, true), "Student created successfully.");
    }

    public async Task<ApiResponse<StudentResponse>> UpdateStudentAsync(int id, UpdateStudentRequest request)
    {
        var student = await _studentRepository.GetByIdAsync(id);
        if (student == null)
            return ApiResponse<StudentResponse>.ErrorResponse($"Student with ID {id} not found.");

        if (string.IsNullOrWhiteSpace(request.FullName))
            return ApiResponse<StudentResponse>.ErrorResponse("FullName is required.");

        if (string.IsNullOrWhiteSpace(request.Email))
            return ApiResponse<StudentResponse>.ErrorResponse("Email is required.");

        if (request.DateOfBirth >= DateTime.Now)
            return ApiResponse<StudentResponse>.ErrorResponse("DateOfBirth must be in the past.");

        var existingEmail = await _studentRepository.GetByEmailAsync(request.Email);
        if (existingEmail != null && existingEmail.StudentId != id)
            return ApiResponse<StudentResponse>.ErrorResponse($"Email '{request.Email}' is already in use by another student.");

        student.FullName = request.FullName;
        student.Email = request.Email;
        student.DateOfBirth = request.DateOfBirth;

        _studentRepository.Update(student);
        await _studentRepository.SaveChangesAsync();

        var updated = await _studentRepository.GetByIdAsync(id);
        return ApiResponse<StudentResponse>.SuccessResponse(
            MapToStudentResponse(updated!, true), "Student updated successfully.");
    }

    public async Task<ApiResponse<bool>> DeleteStudentAsync(int id)
    {
        var student = await _studentRepository.GetByIdAsync(id);
        if (student == null)
            return ApiResponse<bool>.ErrorResponse($"Student with ID {id} not found.");

        try
        {
            _studentRepository.Delete(student);
            await _studentRepository.SaveChangesAsync();
            return ApiResponse<bool>.SuccessResponse(true, "Student deleted successfully.");
        }
        catch (Exception)
        {
            return ApiResponse<bool>.ErrorResponse(
                "Cannot delete student because they have associated enrollments.");
        }
    }

    // ---- Mapping ----

    private static StudentResponse MapToStudentResponse(Student s, bool includeEnrollments)
    {
        return new StudentResponse
        {
            StudentId = s.StudentId,
            FullName = s.FullName,
            Email = s.Email,
            DateOfBirth = s.DateOfBirth,
            Enrollments = includeEnrollments
                ? s.Enrollments.Select(MapToEnrollmentSummary).ToList()
                : null
        };
    }

    private static EnrollmentSummaryResponse MapToEnrollmentSummary(Enrollment e)
    {
        return new EnrollmentSummaryResponse
        {
            EnrollmentId = e.EnrollmentId,
            StudentId = e.StudentId,
            CourseId = e.CourseId,
            EnrollDate = e.EnrollDate,
            Status = e.Status
        };
    }

    private static IQueryable<Student> ApplySorting(IQueryable<Student> queryable, string sort)
    {
        var parts = sort.Split(',', StringSplitOptions.RemoveEmptyEntries);
        IOrderedQueryable<Student>? ordered = null;

        foreach (var part in parts)
        {
            var trimmed = part.Trim();
            var descending = trimmed.StartsWith("-");
            var field = trimmed.TrimStart('-').ToLower();

            IOrderedQueryable<Student> next = (ordered == null)
                ? field switch
                {
                    "studentid" => descending ? queryable.OrderByDescending(s => s.StudentId) : queryable.OrderBy(s => s.StudentId),
                    "fullname"  => descending ? queryable.OrderByDescending(s => s.FullName)  : queryable.OrderBy(s => s.FullName),
                    "email"     => descending ? queryable.OrderByDescending(s => s.Email)      : queryable.OrderBy(s => s.Email),
                    "dateofbirth" => descending ? queryable.OrderByDescending(s => s.DateOfBirth) : queryable.OrderBy(s => s.DateOfBirth),
                    _           => queryable.OrderBy(s => s.StudentId)
                }
                : field switch
                {
                    "studentid" => descending ? ordered.ThenByDescending(s => s.StudentId) : ordered.ThenBy(s => s.StudentId),
                    "fullname"  => descending ? ordered.ThenByDescending(s => s.FullName)  : ordered.ThenBy(s => s.FullName),
                    "email"     => descending ? ordered.ThenByDescending(s => s.Email)      : ordered.ThenBy(s => s.Email),
                    "dateofbirth" => descending ? ordered.ThenByDescending(s => s.DateOfBirth) : ordered.ThenBy(s => s.DateOfBirth),
                    _           => ordered.ThenBy(s => s.StudentId)
                };

            ordered = next;
        }

        return ordered ?? queryable;
    }
}
