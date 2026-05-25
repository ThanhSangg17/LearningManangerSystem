using Microsoft.EntityFrameworkCore;
using PRN232.LearningManagerSystem.Repositories.DBContext;
using PRN232.LearningManagerSystem.Repositories.Entities;
using PRN232.LearningManagerSystem.Repositories.Interfaces;

namespace PRN232.LearningManagerSystem.Repositories.Implements;

public class EnrollmentRepository : IEnrollmentRepository
{
    private readonly Prn232LmsContext _context;

    public EnrollmentRepository(Prn232LmsContext context)
    {
        _context = context;
    }

    public async Task<List<Enrollment>> GetByCourseIdAsync(int courseId, bool includeStudent)
    {
        var query = _context.Enrollments.Where(e => e.CourseId == courseId);
        if (includeStudent)
        {
            query = query.Include(e => e.Student);
        }
        return await query.ToListAsync();
    }

    public Task<IQueryable<Enrollment>> GetQueryableAsync()
    {
        return Task.FromResult(_context.Enrollments.AsQueryable());
    }

    public async Task<Enrollment?> GetByIdAsync(int id)
    {
        return await _context.Enrollments
            .Include(e => e.Student)
            .Include(e => e.Course)
                .ThenInclude(c => c.Semester)
            .Include(e => e.Course)
                .ThenInclude(c => c.Subject)
            .FirstOrDefaultAsync(e => e.EnrollmentId == id);
    }

    public async Task<Enrollment?> GetByStudentAndCourseAsync(int studentId, int courseId)
    {
        return await _context.Enrollments
            .FirstOrDefaultAsync(e => e.StudentId == studentId && e.CourseId == courseId);
    }

    public async Task<bool> ExistsAsync(int id)
    {
        return await _context.Enrollments.AnyAsync(e => e.EnrollmentId == id);
    }

    public async Task AddAsync(Enrollment entity)
    {
        await _context.Enrollments.AddAsync(entity);
    }

    public void Update(Enrollment entity)
    {
        _context.Enrollments.Update(entity);
    }

    public void Delete(Enrollment entity)
    {
        _context.Enrollments.Remove(entity);
    }

    public async Task<int> SaveChangesAsync()
    {
        return await _context.SaveChangesAsync();
    }
}
