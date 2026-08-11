using System.Collections.Generic;
using System.Threading.Tasks;
using TestPlatform.Service.DTOs.Dashboard;

namespace TestPlatform.Service.Interfaces;

public interface IDashboardService
{
    Task<DashboardSummaryDto> GetSummaryAsync();
    Task<IEnumerable<TopTestDto>> GetTopTestsAsync(int count);
    Task<IEnumerable<RecentAttemptDto>> GetRecentAttemptsAsync(int count);
}
