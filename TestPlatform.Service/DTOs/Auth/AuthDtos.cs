using System.ComponentModel.DataAnnotations;
using TestPlatform.Domain.Enums;

namespace TestPlatform.Service.DTOs.Auth;

public class SendOtpDto
{
    [Required(ErrorMessage = "Email manzil kiritilishi shart.")]
    [EmailAddress(ErrorMessage = "Email manzili noto'g'ri shaklda.")]
    public string Email { get; set; } = string.Empty;
}

public class UserRegisterDto
{
    [Required(ErrorMessage = "Full name is required.")]
    [StringLength(100, ErrorMessage = "Full name cannot exceed 100 characters.")]
    public string FullName { get; set; } = string.Empty;

    [Required(ErrorMessage = "Email is required.")]
    [EmailAddress(ErrorMessage = "Invalid email address format.")]
    [StringLength(100, ErrorMessage = "Email cannot exceed 100 characters.")]
    public string Email { get; set; } = string.Empty;

    [Required(ErrorMessage = "Password is required.")]
    [StringLength(100, MinimumLength = 6, ErrorMessage = "Password must be between 6 and 100 characters.")]
    public string Password { get; set; } = string.Empty;

    [Required(ErrorMessage = "Gmail'ga yuborilgan 6 xonali kodni kiriting.")]
    [StringLength(6, MinimumLength = 6, ErrorMessage = "Tasdiqlash kodi 6 xonali bo'lishi kerak.")]
    public string Code { get; set; } = string.Empty;

    public string Role { get; set; } = "Student";
}

public class UserLoginDto
{
    [Required(ErrorMessage = "Email is required.")]
    [EmailAddress(ErrorMessage = "Invalid email address format.")]
    public string Email { get; set; } = string.Empty;

    [Required(ErrorMessage = "Password is required.")]
    public string Password { get; set; } = string.Empty;
}

public class AuthResultDto
{
    public string Token { get; set; } = string.Empty;
    public UserDetailsDto User { get; set; } = null!;
}

public class UserDetailsDto
{
    public Guid Id { get; set; }
    public string FullName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Role { get; set; } = string.Empty;
    public string? AvatarUrl { get; set; }
}
