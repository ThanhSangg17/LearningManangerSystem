using PRN232.LearningManagerSystem.Services.Models.BusinessModels;
using PRN232.LearningManagerSystem.Services.Models.Common;

namespace PRN232.LearningManagerSystem.Services.Interfaces;

public interface ISemesterService
{
    Task<ServicePagedResult<object>> GetSemestersAsync(ServiceListQueryParameters query);
    Task<ServiceResult<SemesterBusinessModel>> GetSemesterByIdAsync(int id);
    Task<ServiceResult<SemesterBusinessModel>> CreateSemesterAsync(SemesterCreateBusinessModel model);
    Task<ServiceResult<SemesterBusinessModel>> UpdateSemesterAsync(int id, SemesterUpdateBusinessModel model);
    Task<ServiceResult<bool>> DeleteSemesterAsync(int id);
}
