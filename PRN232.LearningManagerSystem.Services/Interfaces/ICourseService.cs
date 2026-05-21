using PRN232.LearningManagerSystem.Services.Models.Common;
using PRN232.LearningManagerSystem.Services.Models.Requests;
using PRN232.LearningManagerSystem.Services.Models.Responses;

namespace PRN232.LearningManagerSystem.Services.Interfaces;

public interface ICourseService
{
    Task<PagedResponse<object>> GetCoursesAsync(ListQueryParameters query);
    Task<ApiResponse<CourseResponse>> GetCourseByIdAsync(int id);
    Task<ApiResponse<CourseResponse>> CreateCourseAsync(CreateCourseRequest request);
    Task<ApiResponse<CourseResponse>> UpdateCourseAsync(int id, UpdateCourseRequest request);
    Task<ApiResponse<bool>> DeleteCourseAsync(int id);
}
