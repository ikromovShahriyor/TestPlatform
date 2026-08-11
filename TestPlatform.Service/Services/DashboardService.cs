using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using TestPlatform.Data.Context;
using TestPlatform.Service.DTOs.Dashboard;
using TestPlatform.Service.Interfaces;

namespace TestPlatform.Service.Services;

public class DashboardService : IDashboardService
{
    private readonly AppDbContext _context;

    public DashboardService(AppDbContext context)
    {
        _context = context;
    }

    public async Task<DashboardSummaryDto> GetSummaryAsync()
    {
        var subjectsCount = await _context.Subjects.CountAsync();
        var testsCount = await _context.Tests.CountAsync();
        var publishedTestsCount = await _context.Tests.CountAsync(t => t.IsPublished);
        var questionsCount = await _context.Questions.CountAsync();
        var attemptsCount = await _context.TestAttempts.CountAsync();

        var averagePercentage = 0.0;
        if (attemptsCount > 0)
        {
            var avg = await _context.TestAttempts.AverageAsync(a => a.Percentage);
            averagePercentage = Math.Round((double)avg, 2);
        }

        var passedAttemptsCount = await _context.TestAttempts
            .CountAsync(a => (double)a.Percentage >= (a.Test != null ? a.Test.PassingPercentage : 60));

        var failedAttemptsCount = attemptsCount - passedAttemptsCount;

        return new DashboardSummaryDto
        {
            SubjectsCount = subjectsCount,
            TestsCount = testsCount,
            PublishedTestsCount = publishedTestsCount,
            QuestionsCount = questionsCount,
            AttemptsCount = attemptsCount,
            AveragePercentage = averagePercentage,
            PassedAttemptsCount = passedAttemptsCount,
            FailedAttemptsCount = failedAttemptsCount
        };
    }

    public async Task<IEnumerable<TopTestDto>> GetTopTestsAsync(int count)
    {
        if (count <= 0) count = 5;

        // If there are no attempts, return empty
        if (!await _context.TestAttempts.AnyAsync())
        {
            return new List<TopTestDto>();
        }

        var topTests = await _context.TestAttempts
            .Include(a => a.Test)
                .ThenInclude(t => t.Subject)
            .Where(a => a.Test != null)
            .GroupBy(a => new { a.TestId, a.Test.Title, SubjectName = a.Test.Subject != null ? a.Test.Subject.Name : "" })
            .Select(g => new TopTestDto
            {
                TestId = g.Key.TestId,
                Title = g.Key.Title,
                SubjectName = g.Key.SubjectName,
                AttemptsCount = g.Count(),
                AveragePercentage = Math.Round((double)g.Average(a => a.Percentage), 2)
            })
            .OrderByDescending(t => t.AttemptsCount)
            .Take(count)
            .ToListAsync();

        return topTests;
    }

    public async Task<IEnumerable<RecentAttemptDto>> GetRecentAttemptsAsync(int count)
    {
        if (count <= 0) count = 5;

        var recent = await _context.TestAttempts
            .Include(a => a.Test)
            .OrderByDescending(a => a.PassedAt)
            .Take(count)
            .ToListAsync();

        return recent.Select(a => new RecentAttemptDto
        {
            AttemptId = a.Id,
            StudentName = a.StudentName,
            TestTitle = a.Test != null ? a.Test.Title : "Noma'lum",
            EarnedScore = a.EarnedScore,
            TotalScore = a.TotalScore,
            Percentage = (double)a.Percentage,
            IsPassed = (double)a.Percentage >= (a.Test != null ? a.Test.PassingPercentage : 60),
            PassedAt = a.PassedAt
        }).ToList();
    }
}
