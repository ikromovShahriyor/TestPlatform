using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Threading.Tasks;
using TestPlatform.Service.Interfaces;

namespace TestPlatform.WebApi.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class LeaderboardController : ControllerBase
{
    private readonly ILeaderboardService _leaderboardService;

    public LeaderboardController(ILeaderboardService leaderboardService)
    {
        _leaderboardService = leaderboardService;
    }

    [HttpGet("global")]
    public async Task<IActionResult> GetGlobal([FromQuery] int count = 10)
    {
        var result = await _leaderboardService.GetGlobalLeaderboardAsync(count);
        return Ok(result);
    }

    [HttpGet("test/{testId:guid}")]
    public async Task<IActionResult> GetTestLeaderboard([FromRoute] Guid testId, [FromQuery] int count = 10)
    {
        var result = await _leaderboardService.GetTestLeaderboardAsync(testId, count);
        return Ok(result);
    }
}
