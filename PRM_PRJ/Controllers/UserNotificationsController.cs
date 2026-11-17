using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PRM_PRJ.DTO.Response;
using PRM_PRJ.Models;
using System.Security.Claims;

namespace PRM_PRJ.Controllers
{
    [Authorize]
    [ApiController]
    [Route("api/user-notifications")]
    public class UserNotificationsController : ControllerBase
    {
        private readonly AppDbContext _context;

        public UserNotificationsController(AppDbContext context)
        {
            _context = context;
        }

        [HttpGet]
        public async Task<IActionResult> GetMine()
        {
            var userIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (!Guid.TryParse(userIdClaim, out var userId))
                return Unauthorized(new { message = "Token không hợp lệ" });

            if (User.IsInRole("Admin"))
                return Ok(new List<AdminNotificationItemResponse>());

            var items = await _context.AdminNotifications
                .Include(x => x.CreatedByUser)
                .Include(x => x.UserReads)
                .OrderByDescending(x => x.CreatedAt)
                .Select(x => new AdminNotificationItemResponse
                {
                    Id = x.Id,
                    Title = x.Title,
                    Content = x.Content,
                    CreatedAt = x.CreatedAt.ToLocalTime().ToString("yyyy-MM-dd HH:mm"),
                    CreatedBy = x.CreatedByUser.Username,
                    IsRead = x.UserReads.Any(r => r.UserId == userId)
                })
                .ToListAsync();

            return Ok(items);
        }

        [HttpGet("unread-count")]
        public async Task<IActionResult> GetUnreadCount()
        {
            var userIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (!Guid.TryParse(userIdClaim, out var userId))
                return Unauthorized(new { message = "Token không hợp lệ" });

            if (User.IsInRole("Admin"))
                return Ok(new { unreadCount = 0 });

            var unreadCount = await _context.AdminNotifications
                .CountAsync(x => !x.UserReads.Any(r => r.UserId == userId));

            return Ok(new { unreadCount });
        }

        [HttpPut("{id:int}/read")]
        public async Task<IActionResult> MarkAsRead(int id)
        {
            var userIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (!Guid.TryParse(userIdClaim, out var userId))
                return Unauthorized(new { message = "Token không hợp lệ" });

            var exists = await _context.AdminNotifications.AnyAsync(x => x.Id == id);
            if (!exists)
                return NotFound(new { message = "Không tìm thấy thông báo" });

            var alreadyRead = await _context.UserNotificationReads
                .AnyAsync(x => x.NotificationId == id && x.UserId == userId);

            if (!alreadyRead)
            {
                _context.UserNotificationReads.Add(new UserNotificationRead
                {
                    NotificationId = id,
                    UserId = userId,
                    ReadAt = DateTime.UtcNow
                });

                await _context.SaveChangesAsync();
            }

            return Ok(new { message = "Đã đánh dấu đã đọc" });
        }
    }
}