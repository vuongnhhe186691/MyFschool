using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PRM_PRJ.DTO.Request;
using PRM_PRJ.DTO.Response;
using PRM_PRJ.Models;
using System.Security.Claims;

namespace PRM_PRJ.Controllers
{
    [Authorize(Roles = "Admin")]
    [ApiController]
    [Route("api/admin-notifications")]
    public class AdminNotificationsController : ControllerBase
    {
        private readonly AppDbContext _context;

        public AdminNotificationsController(AppDbContext context)
        {
            _context = context;
        }

        [HttpGet]
        public async Task<IActionResult> GetAll()
        {
            var items = await _context.AdminNotifications
                .Include(x => x.CreatedByUser)
                .OrderByDescending(x => x.CreatedAt)
                .Select(x => new AdminNotificationItemResponse
                {
                    Id = x.Id,
                    Title = x.Title,
                    Content = x.Content,
                    CreatedAt = x.CreatedAt.ToLocalTime().ToString("yyyy-MM-dd HH:mm"),
                    CreatedBy = x.CreatedByUser.Username,
                    IsRead = false
                })
                .ToListAsync();

            return Ok(items);
        }

        [HttpPost]
        public async Task<IActionResult> Create([FromBody] CreateAdminNotificationRequest request)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var userIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (!Guid.TryParse(userIdClaim, out var adminUserId))
                return Unauthorized(new { message = "Token không hợp lệ" });

            var entity = new AdminNotification
            {
                Title = request.Title.Trim(),
                Content = request.Content.Trim(),
                CreatedByUserId = adminUserId,
                CreatedAt = DateTime.UtcNow
            };

            _context.AdminNotifications.Add(entity);
            await _context.SaveChangesAsync();

            return Ok(new { message = "Gửi thông báo thành công" });
        }
    }
}