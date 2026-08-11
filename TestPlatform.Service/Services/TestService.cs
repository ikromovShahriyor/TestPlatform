using Microsoft.EntityFrameworkCore;
using TestPlatform.Data.Context;
using TestPlatform.Domain.Entities;
using TestPlatform.Domain.Enums;
using TestPlatform.Service.DTOs.Tests;
using TestPlatform.Service.DTOs.Pagination;
using TestPlatform.Service.Interfaces;

namespace TestPlatform.Service.Services;

public class TestService : ITestService
{
    private readonly AppDbContext _context;

    public TestService(AppDbContext context)
    {
        _context = context;
    }

    public async Task<TestDetailsDto> CreateAsync(TestCreateDto dto)
    {
        var subject = await _context.Subjects.FindAsync(dto.SubjectId);
        if (subject == null)
            throw new Exception("Subject not found");

        var test = new Test
        {
            SubjectId = dto.SubjectId,
            Title = dto.Title,
            Description = dto.Description,
            PassingPercentage = dto.PassingPercentage,
            DurationMinutes = dto.DurationMinutes > 0 ? dto.DurationMinutes : 15,
            TimeLimitMinutes = dto.TimeLimitMinutes > 0 ? dto.TimeLimitMinutes : (dto.DurationMinutes > 0 ? dto.DurationMinutes : 15),
            IsPublished = dto.IsPublished,
            MaxAttemptsPerStudent = dto.MaxAttemptsPerStudent,
            ShowReviewAfterSubmit = dto.ShowReviewAfterSubmit,
            Difficulty = Enum.TryParse<DifficultyLevel>(dto.Difficulty, true, out var diffVal) ? diffVal : DifficultyLevel.Medium
        };

        if (dto.TopicIds != null && dto.TopicIds.Any())
        {
            test.TestTopics = dto.TopicIds.Select(topicId => new TestTopic
            {
                TopicId = topicId
            }).ToList();
        }

        _context.Tests.Add(test);
        await _context.SaveChangesAsync();

        return await GetByIdAsync(test.Id);
    }

    public async Task<TestDetailsDto> UpdateAsync(Guid id, TestUpdateDto dto)
    {
        var test = await _context.Tests.FindAsync(id);
        if (test == null)
            throw new Exception("Test not found");

        var subject = await _context.Subjects.FindAsync(dto.SubjectId);
        if (subject == null)
            throw new Exception("Subject not found");

        test.SubjectId = dto.SubjectId;
        test.Title = dto.Title;
        test.Description = dto.Description;
        test.PassingPercentage = dto.PassingPercentage;
        test.DurationMinutes = dto.DurationMinutes;
        test.TimeLimitMinutes = dto.TimeLimitMinutes;
        test.MaxAttemptsPerStudent = dto.MaxAttemptsPerStudent;
        test.ShowReviewAfterSubmit = dto.ShowReviewAfterSubmit;
        test.Difficulty = Enum.TryParse<DifficultyLevel>(dto.Difficulty, true, out var dVal) ? dVal : DifficultyLevel.Medium;
 
        // Update topics
        var existingTopics = _context.TestTopics.Where(tt => tt.TestId == id);
        _context.TestTopics.RemoveRange(existingTopics);

        if (dto.TopicIds != null && dto.TopicIds.Any())
        {
            test.TestTopics = dto.TopicIds.Select(topicId => new TestTopic
            {
                TopicId = topicId
            }).ToList();
        }

        await _context.SaveChangesAsync();
        return await GetByIdAsync(id);
    }

    public async Task<bool> DeleteAsync(Guid id)
    {
        var test = await _context.Tests.FindAsync(id);
        if (test == null) return false;

        var attempts = await _context.TestAttempts.Where(ta => ta.TestId == id).ToListAsync();
        _context.TestAttempts.RemoveRange(attempts);

        _context.Tests.Remove(test);
        await _context.SaveChangesAsync();
        return true;
    }

    public async Task<TestDetailsDto> GetByIdAsync(Guid id)
    {
        var test = await _context.Tests
            .Include(t => t.Questions)
                .ThenInclude(q => q.Options)
            .Include(t => t.TestTopics)
                .ThenInclude(tt => tt.Topic)
            .FirstOrDefaultAsync(t => t.Id == id);

        if (test == null)
            throw new Exception("Test not found");

        return new TestDetailsDto
        {
            Id = test.Id,
            Title = test.Title,
            Description = test.Description,
            PassingPercentage = test.PassingPercentage,
            DurationMinutes = test.DurationMinutes,
            TimeLimitMinutes = test.TimeLimitMinutes > 0 ? test.TimeLimitMinutes : test.DurationMinutes,
            IsPublished = test.IsPublished,
            MaxAttemptsPerStudent = test.MaxAttemptsPerStudent,
            ShowReviewAfterSubmit = test.ShowReviewAfterSubmit,
            Difficulty = test.Difficulty.ToString(),
            Topics = test.TestTopics.Select(tt => tt.Topic.Name).ToList(),
            Questions = test.Questions.Select(q => new QuestionItemDto
            {
                Id = q.Id,
                Text = q.Text,
                Points = q.Points,
                Options = q.Options.Select(o => new OptionItemDto
                {
                    Id = o.Id,
                    Text = o.Text
                }).ToList()
            }).ToList()
        };
    }

    public async Task<IEnumerable<TestListDto>> GetAllAsync()
    {
        return await _context.Tests
            .Include(t => t.Subject)
            .Include(t => t.Questions)
            .Include(t => t.TestTopics)
                .ThenInclude(tt => tt.Topic)
            .Select(t => new TestListDto
            {
                Id = t.Id,
                Title = t.Title,
                SubjectName = t.Subject != null ? t.Subject.Name : string.Empty,
                QuestionsCount = t.Questions.Count,
                PassingPercentage = t.PassingPercentage,
                DurationMinutes = t.DurationMinutes,
                TimeLimitMinutes = t.TimeLimitMinutes > 0 ? t.TimeLimitMinutes : t.DurationMinutes,
                IsPublished = t.IsPublished,
                MaxAttemptsPerStudent = t.MaxAttemptsPerStudent,
                Difficulty = t.Difficulty.ToString(),
                Topics = t.TestTopics.Select(tt => tt.Topic.Name).ToList()
            }).ToListAsync();
    }

    public async Task<PagedResultDto<TestListDto>> GetAllPagedAsync(int page, int pageSize, string? search, Guid? subjectId, bool onlyPublished, string? difficulty = null, Guid? topicId = null)
    {
        if (page <= 0) page = 1;
        if (pageSize <= 0) pageSize = 10;
        if (pageSize > 50) pageSize = 50;

        var query = _context.Tests
            .Include(t => t.Subject)
            .Include(t => t.Questions)
            .Include(t => t.TestTopics)
                .ThenInclude(tt => tt.Topic)
            .AsNoTracking()
            .AsQueryable();

        if (onlyPublished)
        {
            query = query.Where(t => t.IsPublished);
        }

        if (subjectId != null && subjectId != Guid.Empty)
        {
            query = query.Where(t => t.SubjectId == subjectId);
        }

        if (!string.IsNullOrWhiteSpace(search))
        {
            var searchLower = search.ToLower();
            query = query.Where(t => t.Title.ToLower().Contains(searchLower) || 
                                     (t.Description != null && t.Description.ToLower().Contains(searchLower)));
        }

        if (!string.IsNullOrWhiteSpace(difficulty) && Enum.TryParse<DifficultyLevel>(difficulty, true, out var diff))
        {
            query = query.Where(t => t.Difficulty == diff);
        }

        if (topicId != null && topicId != Guid.Empty)
        {
            query = query.Where(t => t.TestTopics.Any(tt => tt.TopicId == topicId));
        }

        int totalCount = await query.CountAsync();

        var tests = await query
            .OrderByDescending(t => t.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        var items = tests.Select(t => new TestListDto
        {
            Id = t.Id,
            Title = t.Title,
            SubjectName = t.Subject != null ? t.Subject.Name : string.Empty,
            QuestionsCount = t.Questions.Count,
            PassingPercentage = t.PassingPercentage,
            DurationMinutes = t.DurationMinutes,
            TimeLimitMinutes = t.TimeLimitMinutes > 0 ? t.TimeLimitMinutes : t.DurationMinutes,
            IsPublished = t.IsPublished,
            MaxAttemptsPerStudent = t.MaxAttemptsPerStudent,
            Difficulty = t.Difficulty.ToString(),
            Topics = t.TestTopics.Select(tt => tt.Topic.Name).ToList()
        }).ToList();

        return new PagedResultDto<TestListDto>
        {
            Items = items,
            Page = page,
            PageSize = pageSize,
            TotalCount = totalCount
        };
    }

    public async Task<StudentTestDto> GetForStudentAsync(Guid id)
    {
        var test = await _context.Tests
            .Include(t => t.Questions)
                .ThenInclude(q => q.Options)
            .Include(t => t.TestTopics)
                .ThenInclude(tt => tt.Topic)
            .FirstOrDefaultAsync(t => t.Id == id);

        if (test == null || !test.IsPublished)
            throw new Exception("Test not found or not available");

        // Randomize questions and options order for fairness
        var random = new Random();
        var shuffledQuestions = test.Questions
            .OrderBy(_ => random.Next())
            .Select(q => new QuestionItemDto
            {
                Id = q.Id,
                Text = q.Text,
                Points = q.Points,
                Options = q.Options.OrderBy(_ => random.Next()).Select(o => new OptionItemDto
                {
                    Id = o.Id,
                    Text = o.Text
                }).ToList()
            }).ToList();

        return new StudentTestDto
        {
            Id = test.Id,
            Title = test.Title,
            Description = test.Description,
            PassingPercentage = test.PassingPercentage,
            DurationMinutes = test.DurationMinutes,
            TimeLimitMinutes = test.TimeLimitMinutes > 0 ? test.TimeLimitMinutes : test.DurationMinutes,
            MaxAttemptsPerStudent = test.MaxAttemptsPerStudent,
            Difficulty = test.Difficulty.ToString(),
            Topics = test.TestTopics.Select(tt => tt.Topic.Name).ToList(),
            Questions = shuffledQuestions
        };
    }

    public async Task<bool> TogglePublishStatusAsync(Guid id)
    {
        var test = await _context.Tests.FindAsync(id);
        if (test == null) return false;

        test.IsPublished = !test.IsPublished;
        await _context.SaveChangesAsync();
        return true;
    }
}