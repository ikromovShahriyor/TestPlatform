using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Threading.Tasks;
using TestPlatform.Service.DTOs.Topics;
using TestPlatform.Service.Interfaces;

namespace TestPlatform.WebApi.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class TopicsController : ControllerBase
{
    private readonly ITopicService _topicService;

    public TopicsController(ITopicService topicService)
    {
        _topicService = topicService;
    }

    [HttpGet]
    public async Task<IActionResult> GetAllAsync()
    {
        var result = await _topicService.GetAllAsync();
        return Ok(result);
    }

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetByIdAsync([FromRoute] Guid id)
    {
        var result = await _topicService.GetByIdAsync(id);
        if (result == null) return NotFound(new { message = "Mavzu topilmadi." });
        return Ok(result);
    }

    [HttpPost]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> CreateAsync([FromBody] TopicCreateDto dto)
    {
        try
        {
            var result = await _topicService.CreateAsync(dto);
            return Ok(result);
        }
        catch (Exception ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpPut("{id:guid}")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> UpdateAsync([FromRoute] Guid id, [FromBody] TopicUpdateDto dto)
    {
        try
        {
            var result = await _topicService.UpdateAsync(id, dto);
            return Ok(result);
        }
        catch (Exception ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpDelete("{id:guid}")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> DeleteAsync([FromRoute] Guid id)
    {
        var result = await _topicService.DeleteAsync(id);
        if (!result) return NotFound(new { message = "Mavzu topilmadi yoki o'chirib bo'lmadi." });

        return Ok(new { message = "Mavzu muvaffaqiyatli o'chirildi." });
    }
}
