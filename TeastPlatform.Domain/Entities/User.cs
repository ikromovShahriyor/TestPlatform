using System.ComponentModel.DataAnnotations;
using TestPlatform.Domain.Configuration;
using TestPlatform.Domain.Enums;

namespace TestPlatform.Domain.Entities;

public class User : Auditable
{
    [Required]
    [MaxLength(100)]
    public string FullName { get; set; } = string.Empty;

    [Required]
    [MaxLength(100)]
    public string Email { get; set; } = string.Empty;

    [Required]
    [MaxLength(255)]
    public string PasswordHash { get; set; } = string.Empty;

    public UserRole Role { get; set; } = UserRole.Student;

    public string? AvatarUrl { get; set; }

    public bool IsEmailVerified { get; set; } = false;
}
