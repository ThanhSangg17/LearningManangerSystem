using PRN232.LearningManagerSystem.Services.Models.BusinessModels;
using PRN232.LearningManagerSystem.Services.Models.Common;

namespace PRN232.LearningManagerSystem.Services.Interfaces;

public interface ISubjectService
{
    Task<ServicePagedResult<object>> GetSubjectsAsync(ServiceListQueryParameters query);
    Task<ServiceResult<SubjectBusinessModel>> GetSubjectByIdAsync(int id);
    Task<ServiceResult<SubjectBusinessModel>> CreateSubjectAsync(SubjectCreateBusinessModel model);
    Task<ServiceResult<SubjectBusinessModel>> UpdateSubjectAsync(int id, SubjectUpdateBusinessModel model);
    Task<ServiceResult<bool>> DeleteSubjectAsync(int id);
}
