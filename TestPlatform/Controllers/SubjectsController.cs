using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TestPlatform.Service.Interfaces;
using TestPlatform.Service.DTOs.Subjects;

namespace TestPlatform.WebApi.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize] // All subject actions require authorization
public class SubjectsController : ControllerBase
{
    private readonly ISubjectService _subjectService;

    public SubjectsController(ISubjectService subjectService)
    {
        _subjectService = subjectService;
    }

    [HttpPost]
    [Authorize(Roles = "Admin")] // Only Admin can create subjects
    public async Task<IActionResult> CreateAsync([FromBody] SubjectCreateDto dto)
    {
        var result = await _subjectService.CreateAsync(dto);
        return Ok(result);
    }

    [HttpGet]
    public async Task<IActionResult> GetAllAsync([FromQuery] int page = 1, [FromQuery] int pageSize = 10, [FromQuery] string? search = null)
    {
        var results = await _subjectService.GetAllAsync(page, pageSize, search);
        return Ok(results);
    }

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetByIdAsync([FromRoute] Guid id)
    {
        var result = await _subjectService.GetByIdAsync(id);
        if (result == null) return NotFound(new { message = "Fan topilmadi" });
        return Ok(result);
    }

    [HttpDelete("{id:guid}")]
    [Authorize(Roles = "Admin")] // Only Admin can delete subjects
    public async Task<IActionResult> DeleteAsync([FromRoute] Guid id)
    {
        var result = await _subjectService.DeleteAsync(id);
        if (!result) return NotFound(new { message = "Fan topilmadi yoki o'chirib bo'lmadi" });

        return Ok(new { message = "Fan va unga tegishli barcha testlar muvaffaqiyatli o'chirildi" });
    }
}