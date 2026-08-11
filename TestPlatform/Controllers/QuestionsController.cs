using Microsoft.AspNetCore.Mvc;
using TestPlatform.Service.DTOs.Questions;
using TestPlatform.Service.Interfaces;

namespace TestPlatform.WebApi.Controllers;

[ApiController]
[Route("api")]
[Microsoft.AspNetCore.Authorization.Authorize(Roles = "Admin")]
public class QuestionsController : ControllerBase
{
    private readonly IQuestionService _questionService;

    public QuestionsController(IQuestionService questionService)
    {
        _questionService = questionService;
    }

    [HttpPost("tests/{testId}/questions")]
    public async Task<IActionResult> CreateAsync(Guid testId, [FromBody] QuestionCreateDto dto)
    {
        try
        {
            var result = await _questionService.CreateAsync(testId, dto);
            return StatusCode(201, result);
        }
        catch (Exception ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpPost("tests/{testId}/questions/bulk")]
    public async Task<IActionResult> CreateBulkAsync(Guid testId, [FromBody] List<QuestionCreateDto> dtos)
    {
        try
        {
            var results = await _questionService.CreateBulkAsync(testId, dtos);
            return StatusCode(201, results);
        }
        catch (Exception ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpPost("tests/{testId}/questions/import")]
    public async Task<IActionResult> ImportQuestionsAsync(Guid testId, [FromBody] List<QuestionCreateDto> dtos)
    {
        try
        {
            var result = await _questionService.ImportQuestionsAsync(testId, dtos);
            return Ok(result);
        }
        catch (Exception ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpPut("questions/{id}")]
    public async Task<IActionResult> UpdateAsync(Guid id, [FromBody] QuestionUpdateDto dto)
    {
        try
        {
            var result = await _questionService.UpdateAsync(id, dto);
            return Ok(result);
        }
        catch (Exception ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpDelete("questions/{id}")]
    public async Task<IActionResult> DeleteAsync(Guid id)
    {
        try
        {
            await _questionService.DeleteAsync(id);
            return Ok(new { message = "Savol muvaffaqiyatli o'chirildi." });
        }
        catch (Exception ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }
}