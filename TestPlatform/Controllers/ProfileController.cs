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
