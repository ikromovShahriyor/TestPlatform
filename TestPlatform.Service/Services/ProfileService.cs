using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using TestPlatform.Data.Context;
using TestPlatform.Domain.Entities;
using TestPlatform.Service.DTOs.Profile;
using TestPlatform.Service.Interfaces;

namespace TestPlatform.Service.Services;

public class ProfileService : IProfileService
{
    private readonly AppDbContext _context;
    private readonly IPasswordHasher<User> _passwordHasher;

    public ProfileService(AppDbContext context, IPasswordHasher<User> passwordHasher)
    {
        _context = context;
        _passwordHasher = passwordHasher;
    }

    public async Task<UserProfileDto?> GetProfileAsync(Guid userId)
    {
        var user = await _context.Users.FindAsync(userId);
        if (user == null) return null;

        return new UserProfileDto
        {
            Id = user.Id,
            FullName = user.FullName,
            Email = user.Email,
            Role = user.Role.ToString(),
            AvatarUrl = user.AvatarUrl,
            CreatedAt = user.CreatedAt
        };
    }

    public async Task<UserProfileDto> UpdateProfileAsync(Guid userId, UserProfileUpdateDto dto)
    {
        var user = await _context.Users.FindAsync(userId);
        if (user == null) throw new Exception("Foydalanuvchi topilmadi!");

        if (await _context.Users.AnyAsync(u => u.Id != userId && u.Email == dto.Email.Trim()))
        {
            throw new Exception("Bunday email allaqachon ro'yxatdan o'tgan!");
        }

        user.FullName = dto.FullName.Trim();
        user.Email = dto.Email.Trim();

        if (!string.IsNullOrWhiteSpace(dto.AvatarUrl))
        {
            user.AvatarUrl = dto.AvatarUrl;
        }

        if (!string.IsNullOrWhiteSpace(dto.NewPassword))
        {
            user.PasswordHash = _passwordHasher.HashPassword(user, dto.NewPassword);
        }

        await _context.SaveChangesAsync();

        return new UserProfileDto
        {
            Id = user.Id,
            FullName = user.FullName,
            Email = user.Email,
            Role = user.Role.ToString(),
            AvatarUrl = user.AvatarUrl,
            CreatedAt = user.CreatedAt
        };
    }

    public async Task<IEnumerable<UserAttemptHistoryDto>> GetAttemptsAsync(Guid userId)
    {
        var attempts = await _context.TestAttempts
            .Include(a => a.Test)
                .ThenInclude(t => t.Subject)
            .Where(a => a.UserId == userId)
            .OrderByDescending(a => a.PassedAt)
            .ToListAsync();

        return attempts.Select(a => new UserAttemptHistoryDto
        {
            AttemptId = a.Id,
            TestId = a.TestId,
            TestTitle = a.Test != null ? a.Test.Title : "Noma'lum",
            SubjectName = (a.Test != null && a.Test.Subject != null) ? a.Test.Subject.Name : "Noma'lum",
            EarnedScore = a.EarnedScore,
            TotalScore = a.TotalScore,
            Percentage = (double)a.Percentage,
            DurationSeconds = a.DurationSeconds,
            IsPassed = (double)a.Percentage >= (a.Test != null ? a.Test.PassingPercentage : 60),
            IsExpired = a.IsExpired,
            PassedAt = a.PassedAt
        });
    }

    public async Task<UserAttemptHistoryDto?> GetAttemptByIdAsync(Guid userId, Guid attemptId)
    {
        var attempt = await _context.TestAttempts
            .Include(a => a.Test)
                .ThenInclude(t => t.Subject)
            .FirstOrDefaultAsync(a => a.Id == attemptId && a.UserId == userId);

        if (attempt == null) return null;

        return new UserAttemptHistoryDto
        {
            AttemptId = attempt.Id,
            TestId = attempt.TestId,
            TestTitle = attempt.Test != null ? attempt.Test.Title : "Noma'lum",
            SubjectName = (attempt.Test != null && attempt.Test.Subject != null) ? attempt.Test.Subject.Name : "Noma'lum",
            EarnedScore = attempt.EarnedScore,
            TotalScore = attempt.TotalScore,
            Percentage = (double)attempt.Percentage,
            DurationSeconds = attempt.DurationSeconds,
            IsPassed = (double)attempt.Percentage >= (attempt.Test != null ? attempt.Test.PassingPercentage : 60),
            IsExpired = attempt.IsExpired,
            PassedAt = attempt.PassedAt
        };
    }
}
