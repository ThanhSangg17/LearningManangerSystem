using Microsoft.EntityFrameworkCore;
using PRN232.LearningManagerSystem.Repositories.DBContext;
using PRN232.LearningManagerSystem.Repositories.Entities;
using PRN232.LearningManagerSystem.Repositories.Interfaces;

namespace PRN232.LearningManagerSystem.Repositories.Implements;

public class StudentRepository : IStudentRepository
{
    private readonly Prn232LmsContext _context;

    public StudentRepository(Prn232LmsContext context)
    {
        _context = context;
    }

    public Task<IQueryable<Student>> GetQueryableAsync()
    {
        return Task.FromResult(_context.Students.AsQueryable());
    }

    public async Task<Student?> GetByIdAsync(int id)
    {
        return await _context.Students
            .Include(s => s.Enrollments)
                .ThenInclude(e => e.Course)
                    .ThenInclude(c => c.Semester)
            .Include(s => s.Enrollments)
                .ThenInclude(e => e.Course)
                    .ThenInclude(c => c.Subject)
            .FirstOrDefaultAsync(s => s.StudentId == id);
    }

    public async Task<Student?> GetByEmailAsync(string email)
    {
        return await _context.Students
            .FirstOrDefaultAsync(s => s.Email.ToLower() == email.ToLower());
    }

    public async Task<bool> ExistsAsync(int id)
    {
        return await _context.Students.AnyAsync(s => s.StudentId == id);
    }

    public async Task AddAsync(Student entity)
    {
        await _context.Students.AddAsync(entity);
    }

    public void Update(Student entity)
    {
        _context.Students.Update(entity);
    }

    public void Delete(Student entity)
    {
        _context.Students.Remove(entity);
    }

    public async Task<int> SaveChangesAsync()
    {
        return await _context.SaveChangesAsync();
    }
}
