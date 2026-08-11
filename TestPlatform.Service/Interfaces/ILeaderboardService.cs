using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using TestPlatform.Service.DTOs.Dashboard;

namespace TestPlatform.Service.Interfaces;

public interface ILeaderboardService
{
    Task<IEnumerable<LeaderboardItemDto>> GetTestLeaderboardAsync(Guid testId, int count = 10);
    Task<IEnumerable<GlobalLeaderboardItemDto>> GetGlobalLeaderboardAsync(int count = 10);
}
