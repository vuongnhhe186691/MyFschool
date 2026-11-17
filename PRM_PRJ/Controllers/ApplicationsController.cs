using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PRM_PRJ.DTO.Request;
using PRM_PRJ.DTO.Response;
using PRM_PRJ.Models;
using System.Security.Claims;

namespace PRM_PRJ.Controllers
{
    [Authorize]
    [ApiController]
    [Route("api")]
    public class ApplicationsController : ControllerBase
    {
        private readonly AppDbContext _context;

        public ApplicationsController(AppDbContext context)
        {
            _context = context;
        }

        [HttpGet("applications/my")]
        public async Task<IActionResult> GetMyApplications()
        {
            var userIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (!Guid.TryParse(userIdClaim, out var userId))
                return Unauthorized(new { message = "Token không hợp lệ" });

            if (User.IsInRole("Admin"))
                return BadRequest(new { message = "Admin không dùng chức năng gửi đơn" });

            var items = await _context.Applications
                .Include(x => x.SenderUser)
                .Where(x => x.SenderUserId == userId)
                .OrderByDescending(x => x.CreatedAt)
                .Select(x => new ApplicationItemResponse
                {
                    Id = x.Id,
                    Title = x.Title,
                    Content = x.Content,
                    CreatedAt = x.CreatedAt.ToLocalTime().ToString("yyyy-MM-dd HH:mm"),
                    CreatedBy = x.SenderUser.Username
                })
                .ToListAsync();

            return Ok(items);
        }

        [HttpPost("applications")]
        public async Task<IActionResult> CreateApplication([FromBody] CreateApplicationRequest request)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var userIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (!Guid.TryParse(userIdClaim, out var userId))
                return Unauthorized(new { message = "Token không hợp lệ" });

            if (User.IsInRole("Admin"))
                return BadRequest(new { message = "Admin không thể gửi đơn" });

            var entity = new Application
            {
                Title = request.Title.Trim(),
                Content = request.Content.Trim(),
                SenderUserId = userId,
                CreatedAt = DateTime.UtcNow
            };

            _context.Applications.Add(entity);
            await _context.SaveChangesAsync();

            return Ok(new
            {
                message = "Gửi đơn thành công"
            });
        }

        [Authorize(Roles = "Admin")]
        [HttpGet("admin/applications")]
        public async Task<IActionResult> GetAllForAdmin()
        {
            var items = await _context.Applications
                .Include(x => x.SenderUser)
                .OrderByDescending(x => x.CreatedAt)
                .Select(x => new ApplicationItemResponse
                {
                    Id = x.Id,
                    Title = x.Title,
                    Content = x.Content,
                    CreatedAt = x.CreatedAt.ToLocalTime().ToString("yyyy-MM-dd HH:mm"),
                    CreatedBy = x.SenderUser.Username
                })
                .ToListAsync();

            return Ok(items);
        }
    }
}