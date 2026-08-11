using Microsoft.EntityFrameworkCore;
using TestPlatform.Data.Context;
using TestPlatform.Domain.Entities;
using TestPlatform.Service.DTOs.Subjects;
using TestPlatform.Service.DTOs.Pagination;
using TestPlatform.Service.Interfaces;

namespace TestPlatform.Service.Services;

public class SubjectService : ISubjectService
{
    private readonly AppDbContext _context;

    public SubjectService(AppDbContext context)
    {
        _context = context;
    }

    public async Task<SubjectResultDto> CreateAsync(SubjectCreateDto dto)
    {
        var subject = new Subject
        {
            Name = dto.Name,
            Description = dto.Description
        };

        _context.Subjects.Add(subject);
        await _context.SaveChangesAsync();

        return new SubjectResultDto
        {
            Id = subject.Id,
            Name = subject.Name,
            Description = subject.Description
        };
    }

    public async Task<PagedResultDto<SubjectResultDto>> GetAllAsync(int page, int pageSize, string? search)
    {
        if (page <= 0) page = 1;
        if (pageSize <= 0) pageSize = 10;
        if (pageSize > 50) pageSize = 50;

        var query = _context.Subjects.AsNoTracking().AsQueryable();

        if (!string.IsNullOrWhiteSpace(search))
        {
            var searchLower = search.ToLower();
            query = query.Where(s => s.Name.ToLower().Contains(searchLower) || 
                                     (s.Description != null && s.Description.ToLower().Contains(searchLower)));
        }

        int totalCount = await query.CountAsync();

        var subjects = await query
            .OrderBy(s => s.Name)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        var items = subjects.Select(s => new SubjectResultDto
        {
            Id = s.Id,
            Name = s.Name,
            Description = s.Description
        }).ToList();

        return new PagedResultDto<SubjectResultDto>
        {
            Items = items,
            Page = page,
            PageSize = pageSize,
            TotalCount = totalCount
        };
    }

    public async Task<SubjectResultDto?> GetByIdAsync(Guid id)
    {
        var subject = await _context.Subjects.AsNoTracking().FirstOrDefaultAsync(s => s.Id == id);
        if (subject == null) return null;

        return new SubjectResultDto
        {
            Id = subject.Id,
            Name = subject.Name,
            Description = subject.Description
        };
    }

    public async Task<SubjectResultDto> UpdateAsync(Guid id, SubjectCreateDto dto)
    {
        var subject = await _context.Subjects.FindAsync(id);
        if (subject == null)
            throw new Exception("Fan topilmadi!");

        subject.Name = dto.Name;
        subject.Description = dto.Description;

        await _context.SaveChangesAsync();

        return new SubjectResultDto
        {
            Id = subject.Id,
            Name = subject.Name,
            Description = subject.Description
        };
    }

    public async Task<bool> DeleteAsync(Guid id)
    {
        var subject = await _context.Subjects
            .Include(s => s.Tests)
            .FirstOrDefaultAsync(s => s.Id == id);

        if (subject == null) return false;

        // Fanga tegishli barcha testlar va ularning urinishlarini tozalaymiz
        foreach (var test in subject.Tests.ToList())
        {
            var attempts = await _context.TestAttempts.Where(ta => ta.TestId == test.Id).ToListAsync();
            _context.TestAttempts.RemoveRange(attempts);
            _context.Tests.Remove(test);
        }

        _context.Subjects.Remove(subject);
        await _context.SaveChangesAsync();
        return true;
    }
}