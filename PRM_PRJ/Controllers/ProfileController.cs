using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PRM_PRJ.DTO.Request;
using PRM_PRJ.DTO.Response;
using PRM_PRJ.Models;
using System.Security.Claims;

namespace PRM_PRJ.Controllers
{
    [ApiController]
    [Authorize]
    [Route("api/profile")]
    public class ProfileController : ControllerBase
    {
        private readonly AppDbContext _context;

        public ProfileController(AppDbContext context)
        {
            _context = context;
        }

        [HttpGet("me")]
        public async Task<IActionResult> GetMyProfile()
        {
            var userIdClaim = User.Claims.FirstOrDefault(x => x.Type == ClaimTypes.NameIdentifier)?.Value;
            if (!Guid.TryParse(userIdClaim, out var userId))
                return Unauthorized(new { message = "Token không hợp lệ" });

            var user = await _context.Users
                .Include(x => x.UserRoles)
                    .ThenInclude(x => x.Role)
                .FirstOrDefaultAsync(x => x.Id == userId);

            if (user == null)
                return NotFound(new { message = "Không tìm thấy người dùng" });

            return Ok(new ProfileResponse
            {
                UserId = user.Id.ToString(),
                Username = user.Username,
                Email = user.Email,
                PhoneNumber = user.PhoneNumber ?? string.Empty,
                IsTwoFactorEnabled = user.IsTwoFactorEnabled,
                Roles = user.UserRoles.Select(x => x.Role.Name).ToList()
            });
        }

        [HttpPut("me")]
        public async Task<IActionResult> UpdateMyProfile([FromBody] UpdateProfileRequest request)
        {
            var userIdClaim = User.Claims.FirstOrDefault(x => x.Type == ClaimTypes.NameIdentifier)?.Value;
            if (!Guid.TryParse(userIdClaim, out var userId))
                return Unauthorized(new { message = "Token không hợp lệ" });

            var user = await _context.Users.FirstOrDefaultAsync(x => x.Id == userId);
            if (user == null)
                return NotFound(new { message = "Không tìm thấy người dùng" });

            var email = request.Email?.Trim() ?? string.Empty;
            var phoneNumber = request.PhoneNumber?.Trim() ?? string.Empty;
            var username = request.Username?.Trim() ?? string.Empty;

            if (string.IsNullOrWhiteSpace(username))
                return BadRequest(new { message = "Username không được để trống" });

            if (string.IsNullOrWhiteSpace(email))
                return BadRequest(new { message = "Email không được để trống" });

            var emailExists = await _context.Users.AnyAsync(x => x.Id != userId && x.Email == email);
            if (emailExists)
                return BadRequest(new { message = "Email đã được sử dụng" });

            var phoneExists = await _context.Users.AnyAsync(x => x.Id != userId && x.PhoneNumber == phoneNumber);
            if (phoneExists)
                return BadRequest(new { message = "Số điện thoại đã được sử dụng" });

            user.Username = username;
            user.Email = email;
            user.PhoneNumber = phoneNumber;

            await _context.SaveChangesAsync();

            return Ok(new { message = "Cập nhật profile thành công" });
        }

        [HttpPut("two-factor")]
        public async Task<IActionResult> UpdateTwoFactor([FromBody] UpdateTwoFactorRequest request)
        {
            var userIdClaim = User.Claims.FirstOrDefault(x => x.Type == ClaimTypes.NameIdentifier)?.Value;
            if (!Guid.TryParse(userIdClaim, out var userId))
                return Unauthorized(new { message = "Token không hợp lệ" });

            var user = await _context.Users.FirstOrDefaultAsync(x => x.Id == userId);
            if (user == null)
                return NotFound(new { message = "Không tìm thấy người dùng" });

            if (request.IsEnabled && string.IsNullOrWhiteSpace(user.Email))
            {
                return BadRequest(new { message = "Bạn phải cập nhật email trước khi bật xác thực 2 bước" });
            }

            user.IsTwoFactorEnabled = request.IsEnabled;
            await _context.SaveChangesAsync();

            return Ok(new
            {
                message = request.IsEnabled
                    ? "Đã bật xác thực 2 bước"
                    : "Đã tắt xác thực 2 bước"
            });
        }
    }
}