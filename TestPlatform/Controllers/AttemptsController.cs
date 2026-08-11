using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using TestPlatform.Service.DTOs.Attempts;
using TestPlatform.Service.Interfaces;

namespace TestPlatform.WebApi.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class AttemptsController : ControllerBase
{
    private readonly IAttemptService _attemptService;

    public AttemptsController(IAttemptService attemptService)
    {
        _attemptService = attemptService;
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] AttemptCreateDto dto)
    {
        var result = await _attemptService.CreateAsync(dto);
        return Ok(result);
    }

    [HttpPost("start/{testId:guid}")]
    public async Task<IActionResult> StartAttempt(Guid testId)
    {
        var userIdStr = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (!Guid.TryParse(userIdStr, out var userId))
        {
            return Unauthorized("Siz tizimga kirmagansiz!");
        }

        var studentName = User.FindFirst(ClaimTypes.Name)?.Value ?? "Talaba";
        try
        {
            var attemptId = await _attemptService.StartAttemptAsync(testId, userId, studentName);
            return Ok(new { attemptId });
        }
        catch (Exception ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpPost("{id:guid}/submit")]
    public async Task<IActionResult> SubmitAttempt(Guid id, [FromBody] List<StudentAnswerDto> answers)
    {
        try
        {
            var result = await _attemptService.SubmitAttemptAsync(id, answers);
            return Ok(result);
        }
        catch (Exception ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpGet("{id:guid}/review")]
    public async Task<IActionResult> GetReview(Guid id)
    {
        var userIdStr = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (!Guid.TryParse(userIdStr, out var userId))
        {
            return Unauthorized();
        }

        var role = User.FindFirst(ClaimTypes.Role)?.Value ?? "Student";

        try
        {
            var review = await _attemptService.GetReviewAsync(id, userId, role);
            if (review == null) return NotFound("Natija topilmadi!");
            return Ok(review);
        }
        catch (UnauthorizedAccessException ex)
        {
            return Forbid(ex.Message);
        }
        catch (Exception ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetById(Guid id)
    {
        var result = await _attemptService.GetByIdAsync(id);
        if (result == null) return NotFound("Natija topilmadi!");
        return Ok(result);
    }

    [HttpGet("test/{testId:guid}")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> GetByTestId(Guid testId)
    {
        var results = await _attemptService.GetByTestIdAsync(testId);
        return Ok(results);
    }

    [HttpGet]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> GetAll()
    {
        var results = await _attemptService.GetAllAsync();
        return Ok(results);
    }
}