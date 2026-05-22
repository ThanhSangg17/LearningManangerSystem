using PRN232.LearningManagerSystem.Services.Models.BusinessModels;
using PRN232.LearningManagerSystem.Services.Models.Common;

namespace PRN232.LearningManagerSystem.Services.Interfaces;

public interface IStudentService
{
    Task<ServicePagedResult<object>> GetStudentsAsync(ServiceListQueryParameters query);
    Task<ServiceResult<StudentBusinessModel>> GetStudentByIdAsync(int id);
    Task<ServiceResult<StudentBusinessModel>> CreateStudentAsync(StudentCreateBusinessModel model);
    Task<ServiceResult<StudentBusinessModel>> UpdateStudentAsync(int id, StudentUpdateBusinessModel model);
    Task<ServiceResult<bool>> DeleteStudentAsync(int id);
}
