using Microsoft.EntityFrameworkCore;
using PRN232.LearningManagerSystem.Repositories.DBContext;
using PRN232.LearningManagerSystem.Repositories.Entities;
using PRN232.LearningManagerSystem.Repositories.Interfaces;

namespace PRN232.LearningManagerSystem.Repositories.Implements;

public class SemesterRepository : ISemesterRepository
{
    private readonly Prn232LmsContext _context;

    public SemesterRepository(Prn232LmsContext context)
    {
        _context = context;
    }

    public Task<IQueryable<Semester>> GetQueryableAsync()
    {
        return Task.FromResult(_context.Semesters.AsQueryable());
    }

    public async Task<Semester?> GetByIdAsync(int id)
    {
        return await _context.Semesters
            .Include(s => s.Courses)
                .ThenInclude(c => c.Subject)
            .FirstOrDefaultAsync(s => s.SemesterId == id);
    }

    public async Task<bool> ExistsAsync(int id)
    {
        return await _context.Semesters.AnyAsync(s => s.SemesterId == id);
    }

    public async Task AddAsync(Semester entity)
    {
        await _context.Semesters.AddAsync(entity);
    }

    public void Update(Semester entity)
    {
        _context.Semesters.Update(entity);
    }

    public void Delete(Semester entity)
    {
        _context.Semesters.Remove(entity);
    }

    public async Task<int> SaveChangesAsync()
    {
        return await _context.SaveChangesAsync();
    }
}
