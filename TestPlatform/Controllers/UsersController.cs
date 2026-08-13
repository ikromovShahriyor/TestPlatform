using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using TestPlatform.Data.Context;
using TestPlatform.Domain.Entities;
using TestPlatform.Service.Interfaces;

namespace TestPlatform.WebApi.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize(Roles = "Admin")]
public class UsersController : ControllerBase
{
    private readonly AppDbContext _context;
    private readonly IAuditLogService _auditLogService;

    public UsersController(AppDbContext context, IAuditLogService auditLogService)
    {
        _context = context;
        _auditLogService = auditLogService;
    }

    [HttpGet]
    public async Task<IActionResult> GetAllAsync([FromQuery] int page = 1, [FromQuery] int pageSize = 10, [FromQuery] string? search = null)
    {
        if (page < 1) page = 1;
        if (pageSize < 1 || pageSize > 100) pageSize = 10;

        var query = _context.Users.AsNoTracking().AsQueryable();

        if (!string.IsNullOrWhiteSpace(search))
        {
            var cleanSearch = search.Trim().ToLower();
            query = query.Where(u => u.FullName.ToLower().Contains(cleanSearch) || u.Email.ToLower().Contains(cleanSearch));
        }

        var totalCount = await query.CountAsync();
        var totalPages = (int)Math.Ceiling(totalCount / (double)pageSize);

        var users = await query
            .OrderByDescending(u => u.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(u => new
            {
                u.Id,
                u.FullName,
                u.Email,
                Role = u.Role.ToString(),
                u.AvatarUrl,
                u.IsEmailVerified,
                u.CreatedAt,
                AttemptsCount = _context.TestAttempts.Count(a => a.UserId == u.Id)
            })
            .ToListAsync();

        return Ok(new
        {
            Page = page,
            PageSize = pageSize,
            TotalCount = totalCount,
            TotalPages = totalPages,
            Items = users
        });
    }

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> DeleteUserAsync([FromRoute] Guid id)
    {
        try
        {
            var user = await _context.Users.FirstOrDefaultAsync(u => u.Id == id);
            if (user == null)
            {
                return NotFound(new { message = "Foydalanuvchi topilmadi!" });
            }

            // Prevent self-deletion
            var currentUserIdStr = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (Guid.TryParse(currentUserIdStr, out var currentUserId) && currentUserId == id)
            {
                return BadRequest(new { message = "Joriy kirib turgan administrator o'z hisobini o'chira olmaydi!" });
            }

            // Remove user's test attempts if any
            var userAttempts = await _context.TestAttempts.Where(a => a.UserId == id).ToListAsync();
            if (userAttempts.Any())
            {
                _context.TestAttempts.RemoveRange(userAttempts);
            }

            _context.Users.Remove(user);
            await _context.SaveChangesAsync();

            // Log audit
            await _auditLogService.LogAsync(
                userId: currentUserId,
                action: "O'chirildi",
                entityName: "Foydalanuvchi",
                entityId: id,
                oldValue: $"{user.FullName} ({user.Email})"
            );

            return Ok(new { message = $"Foydalanuvchi '{user.FullName}' muvaffaqiyatli o'chirildi!" });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { message = $"Foydalanuvchini o'chirishda xatolik: {ex.Message}" });
        }
    }
}
