using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TestPlatform.Service.DTOs.Tests;
using TestPlatform.Service.Interfaces;

namespace TestPlatform.WebApi.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize] // Protect all test actions
public class TestsController : ControllerBase
{
    private readonly ITestService _testService;

    public TestsController(ITestService testService)
    {
        _testService = testService;
    }

    [HttpPost]
    [Authorize(Roles = "Admin")] // Only Admin can create tests
    public async Task<IActionResult> CreateAsync([FromBody] TestCreateDto dto)
    {
        try
        {
            var result = await _testService.CreateAsync(dto);
            return Ok(result);
        }
        catch (Exception ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpPut("{id:guid}")]
    [Authorize(Roles = "Admin")] // Only Admin can update tests
    public async Task<IActionResult> UpdateAsync([FromRoute] Guid id, [FromBody] TestUpdateDto dto)
    {
        try
        {
            var result = await _testService.UpdateAsync(id, dto);
            return Ok(result);
        }
        catch (Exception ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpDelete("{id:guid}")]
    [Authorize(Roles = "Admin")] // Only Admin can delete tests
    public async Task<IActionResult> DeleteAsync([FromRoute] Guid id)
    {
        var result = await _testService.DeleteAsync(id);
        if (!result) return NotFound(new { message = "Test not found" });

        return Ok(new { message = "Test successfully deleted" });
    }

    [HttpGet("{id:guid}")]
    [Authorize(Roles = "Admin")] // Admin endpoint to see details with answers
    public async Task<IActionResult> GetByIdAsync([FromRoute] Guid id)
    {
        try
        {
            var result = await _testService.GetByIdAsync(id);
            return Ok(result);
        }
        catch (Exception ex)
        {
            return NotFound(new { message = ex.Message });
        }
    }

    [HttpGet]
    public async Task<IActionResult> GetAllAsync([FromQuery] int page = 1, [FromQuery] int pageSize = 10, [FromQuery] string? search = null, [FromQuery] Guid? subjectId = null, [FromQuery] string? difficulty = null, [FromQuery] Guid? topicId = null)
    {
        // Students see only published, Admins see all
        bool onlyPublished = !User.IsInRole("Admin");
        var result = await _testService.GetAllPagedAsync(page, pageSize, search, subjectId, onlyPublished, difficulty, topicId);
        return Ok(result);
    }

    // Secure endpoint for student quiz taking (randomized questions and options, answers hidden)
    [HttpGet("/api/student-tests/{id:guid}")]
    public async Task<IActionResult> GetForStudentAsync([FromRoute] Guid id)
    {
        try
        {
            var result = await _testService.GetForStudentAsync(id);
            return Ok(result);
        }
        catch (Exception ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    // Toggle publish status (Publish/Unpublish)
    [HttpPatch("{id:guid}/toggle-publish")]
    [Authorize(Roles = "Admin")] // Only Admin can publish/unpublish tests
    public async Task<IActionResult> TogglePublishStatusAsync([FromRoute] Guid id)
    {
        var result = await _testService.TogglePublishStatusAsync(id);
        if (!result) return NotFound(new { message = "Test not found" });

        return Ok(new { message = "Test status updated successfully" });
    }
}