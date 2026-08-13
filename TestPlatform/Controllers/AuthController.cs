using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TestPlatform.Data.Context;
using TestPlatform.Domain.Entities;
using TestPlatform.Domain.Enums;
using TestPlatform.Service.DTOs.Auth;
using TestPlatform.Service.Interfaces;

namespace TestPlatform.WebApi.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AuthController : ControllerBase
{
    private readonly AppDbContext _context;
    private readonly IJwtService _jwtService;
    private readonly IEmailService _emailService;
    private readonly IConfiguration _configuration;
    private readonly PasswordHasher<User> _passwordHasher;

    // In-memory thread-safe OTP store (Email -> (Code, ExpirationTime))
    private static readonly System.Collections.Concurrent.ConcurrentDictionary<string, (string Code, DateTime ExpiresAt)> _otpStore 
        = new System.Collections.Concurrent.ConcurrentDictionary<string, (string Code, DateTime ExpiresAt)>(StringComparer.OrdinalIgnoreCase);

    public AuthController(AppDbContext context, IJwtService jwtService, IEmailService emailService, IConfiguration configuration)
    {
        _context = context;
        _jwtService = jwtService;
        _emailService = emailService;
        _configuration = configuration;
        _passwordHasher = new PasswordHasher<User>();
    }

    [HttpPost("send-otp")]
    public async Task<IActionResult> SendOtp([FromBody] SendOtpDto dto)
    {
        if (await _context.Users.AnyAsync(u => u.Email == dto.Email))
        {
            return BadRequest(new { message = "Ushbu email allaqachon ro'yxatdan o'tgan!" });
        }

        var randomCode = new Random().Next(100000, 999999).ToString();
        var expiresAt = DateTime.UtcNow.AddMinutes(10);

        _otpStore[dto.Email] = (randomCode, expiresAt);

        await _emailService.SendVerificationCodeAsync(dto.Email, randomCode);

        var hasSmtpPassword = !string.IsNullOrWhiteSpace(_configuration["Email:SenderPassword"]);

        return Ok(new 
        { 
            message = hasSmtpPassword ? "Tasdiqlash kodi kiritilgan Gmail manzilga yuborildi!" : $"[Test Rejimi] Kodingiz: {randomCode} (Gmail paroli sozlanmagani uchun ekranga chiqarildi)", 
            email = dto.Email,
            code = hasSmtpPassword ? null : randomCode
        });
    }

    [HttpPost("register")]
    public async Task<IActionResult> Register([FromBody] UserRegisterDto dto)
    {
        if (await _context.Users.AnyAsync(u => u.Email == dto.Email))
        {
            return BadRequest(new { message = "Email allaqachon ro'yxatdan o'tgan!" });
        }

        // Verify OTP Code
        if (!_otpStore.TryGetValue(dto.Email, out var otpData) || otpData.ExpiresAt < DateTime.UtcNow)
        {
            return BadRequest(new { message = "Tasdiqlash kodi muddati o'tgan yoki yuborilmagan! Qaytadan kod so'rang." });
        }

        if (otpData.Code != dto.Code.Trim())
        {
            return BadRequest(new { message = "Gmail tasdiqlash kodi noto'g'ri kiritildi!" });
        }

        // Remove OTP code after successful verification
        _otpStore.TryRemove(dto.Email, out _);

        var user = new User
        {
            FullName = dto.FullName,
            Email = dto.Email,
            Role = UserRole.Student,
            IsEmailVerified = true
        };
        user.PasswordHash = _passwordHasher.HashPassword(user, dto.Password);

        _context.Users.Add(user);
        await _context.SaveChangesAsync();

        var token = _jwtService.GenerateToken(user);

        return Ok(new AuthResultDto
        {
            Token = token,
            User = new UserDetailsDto
            {
                Id = user.Id,
                FullName = user.FullName,
                Email = user.Email,
                Role = user.Role.ToString(),
                AvatarUrl = user.AvatarUrl
            }
        });
    }

    [HttpPost("login")]
    public async Task<IActionResult> Login([FromBody] UserLoginDto dto)
    {
        if (string.IsNullOrWhiteSpace(dto.Email) || string.IsNullOrWhiteSpace(dto.Password))
        {
            return BadRequest(new { message = "Email va parolni kiriting!" });
        }

        var cleanEmail = dto.Email.Trim().ToLower();
        var user = await _context.Users.FirstOrDefaultAsync(u => u.Email.ToLower() == cleanEmail);
        if (user == null)
        {
            return BadRequest(new { message = "Kiritilgan email topilmadi!" });
        }

        var verificationResult = _passwordHasher.VerifyHashedPassword(user, user.PasswordHash, dto.Password);
        if (verificationResult == PasswordVerificationResult.Failed && user.PasswordHash != dto.Password)
        {
            return BadRequest(new { message = "Kiritilgan parol noto'g'ri!" });
        }

        var token = _jwtService.GenerateToken(user);

        return Ok(new AuthResultDto
        {
            Token = token,
            User = new UserDetailsDto
            {
                Id = user.Id,
                FullName = user.FullName,
                Email = user.Email,
                Role = user.Role.ToString(),
                AvatarUrl = user.AvatarUrl
            }
        });
    }
}
