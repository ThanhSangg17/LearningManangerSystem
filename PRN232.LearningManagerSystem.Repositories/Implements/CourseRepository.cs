using Microsoft.EntityFrameworkCore;
using PRN232.LearningManagerSystem.Repositories.DBContext;
using PRN232.LearningManagerSystem.Repositories.Entities;
using PRN232.LearningManagerSystem.Repositories.Interfaces;

namespace PRN232.LearningManagerSystem.Repositories.Implements;

public class CourseRepository : ICourseRepository
{
    private readonly Prn232LmsContext _context;

    public CourseRepository(Prn232LmsContext context)
    {
        _context = context;
    }

    public Task<IQueryable<Course>> GetQueryableAsync()
    {
        return Task.FromResult(_context.Courses.AsQueryable());
    }

    public async Task<Course?> GetByIdAsync(int id)
    {
        return await _context.Courses
            .Include(c => c.Semester)
            .Include(c => c.Subject)
            .Include(c => c.Enrollments)
                .ThenInclude(e => e.Student)
            .FirstOrDefaultAsync(c => c.CourseId == id);
    }

    public async Task<bool> ExistsAsync(int id)
    {
        return await _context.Courses.AnyAsync(c => c.CourseId == id);
    }

    public async Task AddAsync(Course entity)
    {
        await _context.Courses.AddAsync(entity);
    }

    public void Update(Course entity)
    {
        _context.Courses.Update(entity);
    }

    public void Delete(Course entity)
    {
        _context.Courses.Remove(entity);
    }

    public async Task<int> SaveChangesAsync()
    {
        return await _context.SaveChangesAsync();
    }
}
