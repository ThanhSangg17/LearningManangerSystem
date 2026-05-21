using PRN232.LearningManagerSystem.Repositories.Entities;

namespace PRN232.LearningManagerSystem.Repositories.Interfaces;

public interface ICourseRepository
{
    Task<IQueryable<Course>> GetQueryableAsync();
    Task<Course?> GetByIdAsync(int id);
    Task<bool> ExistsAsync(int id);
    Task AddAsync(Course entity);
    void Update(Course entity);
    void Delete(Course entity);
    Task<int> SaveChangesAsync();
}
