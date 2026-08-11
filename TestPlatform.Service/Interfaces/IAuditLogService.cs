using TestPlatform.Service.DTOs.AuditLogs;
using TestPlatform.Service.DTOs.Pagination;

namespace TestPlatform.Service.Interfaces;

public interface IAuditLogService
{
    Task LogAsync(Guid userId, string action, string entityName, Guid entityId, string? oldValue = null, string? newValue = null);
    Task<PagedResultDto<AuditLogDto>> GetAllPagedAsync(int page = 1, int pageSize = 10, string? action = null, string? entityName = null, Guid? userId = null);
}
