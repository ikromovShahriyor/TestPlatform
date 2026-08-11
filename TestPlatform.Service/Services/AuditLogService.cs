using Microsoft.EntityFrameworkCore;
using TestPlatform.Data.Context;
using TestPlatform.Domain.Entities;
using TestPlatform.Service.DTOs.AuditLogs;
using TestPlatform.Service.DTOs.Pagination;
using TestPlatform.Service.Interfaces;

namespace TestPlatform.Service.Services;

public class AuditLogService : IAuditLogService
{
    private readonly AppDbContext _context;

    public AuditLogService(AppDbContext context)
    {
        _context = context;
    }

    public async Task LogAsync(Guid userId, string action, string entityName, Guid entityId, string? oldValue = null, string? newValue = null)
    {
        var log = new AuditLog
        {
            UserId = userId,
            Action = action,
            EntityName = entityName,
            EntityId = entityId,
            OldValue = oldValue,
            NewValue = newValue
        };

        _context.AuditLogs.Add(log);
        await _context.SaveChangesAsync();
    }

    public async Task<PagedResultDto<AuditLogDto>> GetAllPagedAsync(int page = 1, int pageSize = 10, string? action = null, string? entityName = null, Guid? userId = null)
    {
        if (page <= 0) page = 1;
        if (pageSize <= 0) pageSize = 10;
        if (pageSize > 50) pageSize = 50;

        var query = _context.AuditLogs
            .Include(a => a.User)
            .AsNoTracking()
            .AsQueryable();

        if (userId.HasValue && userId.Value != Guid.Empty)
        {
            query = query.Where(a => a.UserId == userId.Value);
        }

        if (!string.IsNullOrWhiteSpace(action))
        {
            var actionLower = action.ToLower();
            query = query.Where(a => a.Action.ToLower().Contains(actionLower));
        }

        if (!string.IsNullOrWhiteSpace(entityName))
        {
            var entityLower = entityName.ToLower();
            query = query.Where(a => a.EntityName.ToLower().Contains(entityLower));
        }

        int totalCount = await query.CountAsync();

        var items = await query
            .OrderByDescending(a => a.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(a => new AuditLogDto
            {
                Id = a.Id,
                UserId = a.UserId,
                UserName = a.User != null ? a.User.FullName : "Noma'lum",
                Action = a.Action,
                EntityName = a.EntityName,
                EntityId = a.EntityId,
                OldValue = a.OldValue,
                NewValue = a.NewValue,
                CreatedAt = a.CreatedAt
            })
            .ToListAsync();

        return new PagedResultDto<AuditLogDto>
        {
            Items = items,
            Page = page,
            PageSize = pageSize,
            TotalCount = totalCount
        };
    }
}
