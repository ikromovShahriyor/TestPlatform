using System;
using System.ComponentModel.DataAnnotations;

namespace TestPlatform.Service.DTOs.Profile;

public class UserProfileDto
{
    public Guid Id { get; set; }
    public string FullName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Role { get; set; } = string.Empty;
    public string? AvatarUrl { get; set; }
    public DateTime CreatedAt { get; set; }
}

public class UserProfileUpdateDto
{
    [Required(ErrorMessage = "Ism va Familiya kiritilishi shart.")]
    [StringLength(100, ErrorMessage = "Ism va Familiya 100 ta belgidan oshmasligi kerak.")]
    public string FullName { get; set; } = string.Empty;

    [EmailAddress(ErrorMessage = "Noto'g'ri email format.")]
    [StringLength(100, ErrorMessage = "Email 100 ta belgidan oshmasligi kerak.")]
    public string Email { get; set; } = string.Empty;

    [MinLength(6, ErrorMessage = "Yangi parol kamida 6 ta belgidan iborat bo'lishi kerak.")]
    [StringLength(100, ErrorMessage = "Yangi parol 100 ta belgidan oshmasligi kerak.")]
    public string? NewPassword { get; set; }

    public string? AvatarUrl { get; set; }
}

public class UserAttemptHistoryDto
{
    public Guid AttemptId { get; set; }
    public Guid TestId { get; set; }
    public string TestTitle { get; set; } = string.Empty;
    public string SubjectName { get; set; } = string.Empty;
    public int EarnedScore { get; set; }
    public int TotalScore { get; set; }
    public double Percentage { get; set; }
    public int DurationSeconds { get; set; }
    public bool IsPassed { get; set; }
    public bool IsExpired { get; set; }
    public DateTime PassedAt { get; set; }
}
