using PRN232.LearningManagerSystem.Services.Models.Common;
using PRN232.LearningManagerSystem.Services.Models.Requests;
using PRN232.LearningManagerSystem.Services.Models.Responses;

namespace PRN232.LearningManagerSystem.Services.Interfaces;

public interface IStudentService
{
    Task<PagedResponse<object>> GetStudentsAsync(ListQueryParameters query);
    Task<ApiResponse<StudentResponse>> GetStudentByIdAsync(int id);
    Task<ApiResponse<StudentResponse>> CreateStudentAsync(CreateStudentRequest request);
    Task<ApiResponse<StudentResponse>> UpdateStudentAsync(int id, UpdateStudentRequest request);
    Task<ApiResponse<bool>> DeleteStudentAsync(int id);
}
