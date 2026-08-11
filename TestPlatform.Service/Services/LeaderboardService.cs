using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using TestPlatform.Data.Context;
using TestPlatform.Service.DTOs.Dashboard;
using TestPlatform.Service.Interfaces;

namespace TestPlatform.Service.Services;

public class LeaderboardService : ILeaderboardService
{
    private readonly AppDbContext _context;

    public LeaderboardService(AppDbContext context)
    {
        _context = context;
    }

    public async Task<IEnumerable<LeaderboardItemDto>> GetTestLeaderboardAsync(Guid testId, int count = 10)
    {
        if (count <= 0) count = 10;

        var attempts = await _context.TestAttempts
            .Where(a => a.TestId == testId && a.SubmittedAt != null)
            .ToListAsync();

        var groupedAttempts = attempts
            .GroupBy(a => a.UserId.HasValue ? a.UserId.Value.ToString() : a.StudentName)
            .Select(g => g
                .OrderByDescending(a => a.Percentage)
                .ThenByDescending(a => a.EarnedScore)
                .ThenBy(a => a.DurationSeconds)
                .ThenBy(a => a.PassedAt)
                .First())
            .OrderByDescending(a => a.Percentage)
            .ThenByDescending(a => a.EarnedScore)
            .ThenBy(a => a.DurationSeconds)
            .Take(count)
            .ToList();

        var result = new List<LeaderboardItemDto>();
        int rank = 1;
        foreach (var a in groupedAttempts)
        {
            result.Add(new LeaderboardItemDto
            {
                Rank = rank++,
                StudentName = a.StudentName,
                Score = a.EarnedScore,
                TotalScore = a.TotalScore,
                Percentage = (double)a.Percentage,
                DurationSeconds = a.DurationSeconds,
                PassedAt = a.PassedAt
            });
        }

        return result;
    }

    public async Task<IEnumerable<GlobalLeaderboardItemDto>> GetGlobalLeaderboardAsync(int count = 10)
    {
        if (count <= 0) count = 10;

        var attempts = await _context.TestAttempts
            .Where(a => a.SubmittedAt != null)
            .ToListAsync();

        if (!attempts.Any()) return new List<GlobalLeaderboardItemDto>();

        var grouped = attempts
            .GroupBy(a => a.UserId.HasValue ? a.UserId.Value.ToString() : a.StudentName)
            .Select(g => new
            {
                StudentName = g.First().StudentName,
                AttemptsCount = g.Count(),
                AveragePercentage = Math.Round((double)g.Average(a => a.Percentage), 2)
            })
            .OrderByDescending(x => x.AveragePercentage)
            .Take(count)
            .ToList();

        var result = new List<GlobalLeaderboardItemDto>();
        int rank = 1;
        foreach (var item in grouped)
        {
            result.Add(new GlobalLeaderboardItemDto
            {
                Rank = rank++,
                StudentName = item.StudentName,
                AttemptsCount = item.AttemptsCount,
                AveragePercentage = item.AveragePercentage
            });
        }

        return result;
    }
}
