using PRN232.LearningManagerSystem.Repositories.Entities;

namespace PRN232.LearningManagerSystem.Repositories.Interfaces;

public interface ISubjectRepository
{
    Task<IQueryable<Subject>> GetQueryableAsync();
    Task<Subject?> GetByIdAsync(int id);
    Task<Subject?> GetByCodeAsync(string subjectCode);
    Task<bool> ExistsAsync(int id);
    Task AddAsync(Subject entity);
    void Update(Subject entity);
    void Delete(Subject entity);
    Task<int> SaveChangesAsync();
}
