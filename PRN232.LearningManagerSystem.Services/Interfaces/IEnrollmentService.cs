using PRN232.LearningManagerSystem.Services.Models.Common;
using PRN232.LearningManagerSystem.Services.Models.Requests;
using PRN232.LearningManagerSystem.Services.Models.Responses;

namespace PRN232.LearningManagerSystem.Services.Interfaces;

public interface IEnrollmentService
{
    Task<PagedResponse<object>> GetEnrollmentsAsync(ListQueryParameters query);
    Task<ApiResponse<EnrollmentResponse>> GetEnrollmentByIdAsync(int id);
    Task<ApiResponse<EnrollmentResponse>> CreateEnrollmentAsync(CreateEnrollmentRequest request);
    Task<ApiResponse<EnrollmentResponse>> UpdateEnrollmentAsync(int id, UpdateEnrollmentRequest request);
    Task<ApiResponse<bool>> DeleteEnrollmentAsync(int id);
}
