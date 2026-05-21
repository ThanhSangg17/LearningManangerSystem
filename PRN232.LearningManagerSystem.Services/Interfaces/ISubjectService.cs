using PRN232.LearningManagerSystem.Services.Models.Common;
using PRN232.LearningManagerSystem.Services.Models.Requests;
using PRN232.LearningManagerSystem.Services.Models.Responses;

namespace PRN232.LearningManagerSystem.Services.Interfaces;

public interface ISubjectService
{
    Task<PagedResponse<object>> GetSubjectsAsync(ListQueryParameters query);
    Task<ApiResponse<SubjectResponse>> GetSubjectByIdAsync(int id);
    Task<ApiResponse<SubjectResponse>> CreateSubjectAsync(CreateSubjectRequest request);
    Task<ApiResponse<SubjectResponse>> UpdateSubjectAsync(int id, UpdateSubjectRequest request);
    Task<ApiResponse<bool>> DeleteSubjectAsync(int id);
}
