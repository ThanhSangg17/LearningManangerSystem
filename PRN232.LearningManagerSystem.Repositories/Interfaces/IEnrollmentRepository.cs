using PRN232.LearningManagerSystem.Repositories.Entities;

namespace PRN232.LearningManagerSystem.Repositories.Interfaces;

public interface IEnrollmentRepository
{
    Task<IQueryable<Enrollment>> GetQueryableAsync();
    Task<Enrollment?> GetByIdAsync(int id);
    Task<Enrollment?> GetByStudentAndCourseAsync(int studentId, int courseId);
    Task<bool> ExistsAsync(int id);
    Task AddAsync(Enrollment entity);
    void Update(Enrollment entity);
    void Delete(Enrollment entity);
    Task<int> SaveChangesAsync();
}
