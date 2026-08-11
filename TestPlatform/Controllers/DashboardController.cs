using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Threading.Tasks;
using TestPlatform.Service.Interfaces;

namespace TestPlatform.WebApi.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize(Roles = "Admin")]
public class DashboardController : ControllerBase
{
    private readonly IDashboardService _dashboardService;

    public DashboardController(IDashboardService dashboardService)
    {
        _dashboardService = dashboardService;
    }

    [HttpGet("summary")]
    public async Task<IActionResult> GetSummary()
    {
        var result = await _dashboardService.GetSummaryAsync();
        return Ok(result);
    }

    [HttpGet("top-tests")]
    public async Task<IActionResult> GetTopTests([FromQuery] int count = 5)
    {
        var result = await _dashboardService.GetTopTestsAsync(count);
        return Ok(result);
    }

    [HttpGet("recent-attempts")]
    public async Task<IActionResult> GetRecentAttempts([FromQuery] int count = 5)
    {
        var result = await _dashboardService.GetRecentAttemptsAsync(count);
        return Ok(result);
    }
}
