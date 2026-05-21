using Microsoft.EntityFrameworkCore;
using PRN232.LearningManagerSystem.Repositories.DBContext;
using PRN232.LearningManagerSystem.Repositories.Entities;
using PRN232.LearningManagerSystem.Repositories.Interfaces;

namespace PRN232.LearningManagerSystem.Repositories.Implements;

public class SubjectRepository : ISubjectRepository
{
    private readonly Prn232LmsContext _context;

    public SubjectRepository(Prn232LmsContext context)
    {
        _context = context;
    }

    public Task<IQueryable<Subject>> GetQueryableAsync()
    {
        return Task.FromResult(_context.Subjects.AsQueryable());
    }

    public async Task<Subject?> GetByIdAsync(int id)
    {
        return await _context.Subjects
            .Include(s => s.Courses)
                .ThenInclude(c => c.Semester)
            .FirstOrDefaultAsync(s => s.SubjectId == id);
    }

    public async Task<Subject?> GetByCodeAsync(string subjectCode)
    {
        return await _context.Subjects
            .FirstOrDefaultAsync(s => s.SubjectCode.ToLower() == subjectCode.ToLower());
    }

    public async Task<bool> ExistsAsync(int id)
    {
        return await _context.Subjects.AnyAsync(s => s.SubjectId == id);
    }

    public async Task AddAsync(Subject entity)
    {
        await _context.Subjects.AddAsync(entity);
    }

    public void Update(Subject entity)
    {
        _context.Subjects.Update(entity);
    }

    public void Delete(Subject entity)
    {
        _context.Subjects.Remove(entity);
    }

    public async Task<int> SaveChangesAsync()
    {
        return await _context.SaveChangesAsync();
    }
}
