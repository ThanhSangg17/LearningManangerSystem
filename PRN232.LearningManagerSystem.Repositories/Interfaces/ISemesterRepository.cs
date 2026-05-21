using PRN232.LearningManagerSystem.Repositories.Entities;

namespace PRN232.LearningManagerSystem.Repositories.Interfaces;

public interface ISemesterRepository
{
    Task<IQueryable<Semester>> GetQueryableAsync();
    Task<Semester?> GetByIdAsync(int id);
    Task<bool> ExistsAsync(int id);
    Task AddAsync(Semester entity);
    void Update(Semester entity);
    void Delete(Semester entity);
    Task<int> SaveChangesAsync();
}
