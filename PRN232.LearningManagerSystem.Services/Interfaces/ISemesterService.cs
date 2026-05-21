using PRN232.LearningManagerSystem.Services.Models.Common;
using PRN232.LearningManagerSystem.Services.Models.Requests;
using PRN232.LearningManagerSystem.Services.Models.Responses;

namespace PRN232.LearningManagerSystem.Services.Interfaces;

public interface ISemesterService
{
    Task<PagedResponse<object>> GetSemestersAsync(ListQueryParameters query);
    Task<ApiResponse<SemesterResponse>> GetSemesterByIdAsync(int id);
    Task<ApiResponse<SemesterResponse>> CreateSemesterAsync(CreateSemesterRequest request);
    Task<ApiResponse<SemesterResponse>> UpdateSemesterAsync(int id, UpdateSemesterRequest request);
    Task<ApiResponse<bool>> DeleteSemesterAsync(int id);
}
