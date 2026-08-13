using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Security.Claims;
using System.Threading.Tasks;
using TestPlatform.Service.DTOs.Profile;
using TestPlatform.Service.Interfaces;

namespace TestPlatform.WebApi.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class ProfileController : ControllerBase
{
    private readonly IProfileService _profileService;

    public ProfileController(IProfileService profileService)
    {
        _profileService = profileService;
    }

    [HttpGet]
    public async Task<IActionResult> GetProfileAsync()
    {
        var userIdStr = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrEmpty(userIdStr) || !Guid.TryParse(userIdStr, out var userId))
        {
            return Unauthorized(new { message = "Foydalanuvchi identifikatori topilmadi." });
        }

        var profile = await _profileService.GetProfileAsync(userId);
        if (profile == null) return NotFound(new { message = "Profil topilmadi." });

        return Ok(profile);
    }

    [HttpPut]
    public async Task<IActionResult> UpdateProfileAsync([FromBody] UserProfileUpdateDto dto)
    {
        var userIdStr = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrEmpty(userIdStr) || !Guid.TryParse(userIdStr, out var userId))
        {
            return Unauthorized(new { message = "Foydalanuvchi identifikatori topilmadi." });
        }

        try
        {
            var result = await _profileService.UpdateProfileAsync(userId, dto);
            return Ok(result);
        }
        catch (Exception ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpPost("upload-avatar")]
    public async Task<IActionResult> UploadAvatar([FromForm] IFormFile file, [FromServices] IWebHostEnvironment env)
    {
        var userIdStr = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrEmpty(userIdStr) || !Guid.TryParse(userIdStr, out var userId))
        {
            return Unauthorized(new { message = "Foydalanuvchi identifikatori topilmadi." });
        }

        if (file == null || file.Length == 0)
        {
            return BadRequest(new { message = "Fayl tanlanmagan!" });
        }

        var allowedExtensions = new[] { ".jpg", ".jpeg", ".png", ".webp" };
        var extension = Path.GetExtension(file.FileName).ToLowerInvariant();

        if (!allowedExtensions.Contains(extension))
        {
            return BadRequest(new { message = "Faqat rasm fayllari (.jpg, .jpeg, .png, .webp) ruxsat etilgan!" });
        }

        var uploadsFolder = Path.Combine(env.WebRootPath ?? Path.Combine(Directory.GetCurrentDirectory(), "wwwroot"), "uploads", "avatars");
        if (!Directory.Exists(uploadsFolder))
        {
            Directory.CreateDirectory(uploadsFolder);
        }

        var fileName = $"{userId}_{Guid.NewGuid()}{extension}";
        var filePath = Path.Combine(uploadsFolder, fileName);

        using (var stream = new FileStream(filePath, FileMode.Create))
        {
            await file.CopyToAsync(stream);
        }

        var avatarUrl = $"/uploads/avatars/{fileName}";

        var currentProfile = await _profileService.GetProfileAsync(userId);
        if (currentProfile != null)
        {
            await _profileService.UpdateProfileAsync(userId, new UserProfileUpdateDto
            {
                FullName = currentProfile.FullName,
                Email = currentProfile.Email,
                AvatarUrl = avatarUrl
            });
        }

        return Ok(new { avatarUrl, message = "Profil rasmi muvaffaqiyatli yuklandi!" });
    }

    [HttpGet("attempts")]
    public async Task<IActionResult> GetAttemptsAsync()
    {
        var userIdStr = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrEmpty(userIdStr) || !Guid.TryParse(userIdStr, out var userId))
        {
            return Unauthorized(new { message = "Foydalanuvchi identifikatori topilmadi." });
        }

        var attempts = await _profileService.GetAttemptsAsync(userId);
        return Ok(attempts);
    }

    [HttpGet("attempts/{attemptId:guid}")]
    public async Task<IActionResult> GetAttemptByIdAsync(Guid attemptId)
    {
        var userIdStr = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrEmpty(userIdStr) || !Guid.TryParse(userIdStr, out var userId))
        {
            return Unauthorized(new { message = "Foydalanuvchi identifikatori topilmadi." });
        }

        var attempt = await _profileService.GetAttemptByIdAsync(userId, attemptId);
        if (attempt == null) return NotFound(new { message = "Urinish topilmadi yoki sizga tegishli emas." });

        return Ok(attempt);
    }
}
