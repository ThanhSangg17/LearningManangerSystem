using PRN232.LearningManagerSystem.Services.Models.BusinessModels;
using PRN232.LearningManagerSystem.Services.Models.Common;

namespace PRN232.LearningManagerSystem.Services.Interfaces;

public interface ICourseService
{
    Task<ServicePagedResult<object>> GetCoursesAsync(ServiceListQueryParameters query);
    Task<ServiceResult<CourseBusinessModel>> GetCourseByIdAsync(int id);
    Task<ServiceResult<CourseBusinessModel>> CreateCourseAsync(CourseCreateBusinessModel model);
    Task<ServiceResult<CourseBusinessModel>> UpdateCourseAsync(int id, CourseUpdateBusinessModel model);
    Task<ServiceResult<bool>> DeleteCourseAsync(int id);
}
