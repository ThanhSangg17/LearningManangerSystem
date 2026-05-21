using PRN232.LearningManagerSystem.Repositories.Entities;

namespace PRN232.LearningManagerSystem.Repositories.Interfaces;

public interface IStudentRepository
{
    Task<IQueryable<Student>> GetQueryableAsync();
    Task<Student?> GetByIdAsync(int id);
    Task<Student?> GetByEmailAsync(string email);
    Task<bool> ExistsAsync(int id);
    Task AddAsync(Student entity);
    void Update(Student entity);
    void Delete(Student entity);
    Task<int> SaveChangesAsync();
}
