using PRN232.LearningManagerSystem.Services.Models.BusinessModels;
using PRN232.LearningManagerSystem.Services.Models.Common;

namespace PRN232.LearningManagerSystem.Services.Interfaces;

public interface IEnrollmentService
{
    Task<ServicePagedResult<object>> GetEnrollmentsAsync(ServiceListQueryParameters query);
    Task<ServiceResult<EnrollmentBusinessModel>> GetEnrollmentByIdAsync(int id);
    Task<ServiceResult<EnrollmentBusinessModel>> CreateEnrollmentAsync(EnrollmentCreateBusinessModel model);
    Task<ServiceResult<EnrollmentBusinessModel>> UpdateEnrollmentAsync(int id, EnrollmentUpdateBusinessModel model);
    Task<ServiceResult<bool>> DeleteEnrollmentAsync(int id);
}
