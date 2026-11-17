using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PRM_PRJ.DTO.Request;
using PRM_PRJ.DTO.Response;
using PRM_PRJ.Models;

namespace PRM_PRJ.Controllers
{
    [Authorize(Roles = "Admin")]
    [ApiController]
    [Route("api/admin/users")]
    public class AdminUsersController : ControllerBase
    {
        private readonly AppDbContext _context;

        public AdminUsersController(AppDbContext context)
        {
            _context = context;
        }

        [HttpGet]
        public async Task<IActionResult> GetUsers()
        {
            var users = await _context.Users
                .Include(x => x.UserRoles)
                    .ThenInclude(x => x.Role)
                .OrderBy(x => x.Username)
                .Select(x => new AdminUserItemResponse
                {
                    Id = x.Id,
                    Username = x.Username,
                    Email = x.Email,
                    PhoneNumber = x.PhoneNumber ?? string.Empty,
                    IsActive = x.IsActive,
                    Roles = x.UserRoles.Select(ur => ur.Role.Name).OrderBy(r => r).ToList()
                })
                .ToListAsync();

            return Ok(users);
        }

        [HttpGet("roles")]
        public async Task<IActionResult> GetRoles()
        {
            var roles = await _context.Roles
                .OrderBy(x => x.Name)
                .Select(x => x.Name)
                .ToListAsync();

            return Ok(roles);
        }

        [HttpPost]
        public async Task<IActionResult> CreateUser([FromBody] AdminUpsertUserRequest request)
        {
            var normalizedPhone = request.PhoneNumber.Trim();
            var normalizedEmail = request.Email.Trim().ToLowerInvariant();

            if (string.IsNullOrWhiteSpace(request.Password))
                return BadRequest(new { message = "Password is required when creating a user" });

            if (await _context.Users.AnyAsync(x => x.PhoneNumber == normalizedPhone))
                return BadRequest(new { message = "Phone number already exists" });

            if (await _context.Users.AnyAsync(x => x.Email == normalizedEmail))
                return BadRequest(new { message = "Email already exists" });

            var roles = await ResolveRolesAsync(request.Roles);
            if (roles.Count == 0)
                return BadRequest(new { message = "At least one valid role is required" });

            var user = new User
            {
                Username = request.Username.Trim(),
                Email = normalizedEmail,
                PhoneNumber = normalizedPhone,
                PasswordHash = BCrypt.Net.BCrypt.HashPassword(request.Password.Trim()),
                IsActive = request.IsActive
            };

            _context.Users.Add(user);

            foreach (var role in roles)
            {
                _context.UserRoles.Add(new UserRole
                {
                    User = user,
                    RoleId = role.Id
                });
            }

            await _context.SaveChangesAsync();
            return Ok(new { message = "User created successfully" });
        }

        [HttpPut("{id:guid}")]
        public async Task<IActionResult> UpdateUser(Guid id, [FromBody] AdminUpsertUserRequest request)
        {
            var user = await _context.Users
                .Include(x => x.UserRoles)
                .FirstOrDefaultAsync(x => x.Id == id);

            if (user == null)
                return NotFound(new { message = "User not found" });

            var normalizedPhone = request.PhoneNumber.Trim();
            var normalizedEmail = request.Email.Trim().ToLowerInvariant();

            if (await _context.Users.AnyAsync(x => x.Id != id && x.PhoneNumber == normalizedPhone))
                return BadRequest(new { message = "Phone number already exists" });

            if (await _context.Users.AnyAsync(x => x.Id != id && x.Email == normalizedEmail))
                return BadRequest(new { message = "Email already exists" });

            var roles = await ResolveRolesAsync(request.Roles);
            if (roles.Count == 0)
                return BadRequest(new { message = "At least one valid role is required" });

            user.Username = request.Username.Trim();
            user.Email = normalizedEmail;
            user.PhoneNumber = normalizedPhone;
            user.IsActive = request.IsActive;

            if (!string.IsNullOrWhiteSpace(request.Password))
                user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(request.Password.Trim());

            _context.UserRoles.RemoveRange(user.UserRoles);

            foreach (var role in roles)
            {
                _context.UserRoles.Add(new UserRole
                {
                    UserId = user.Id,
                    RoleId = role.Id
                });
            }

            await _context.SaveChangesAsync();
            return Ok(new { message = "User updated successfully" });
        }

        [HttpDelete("{id:guid}")]
        public async Task<IActionResult> DeleteUser(Guid id)
        {
            var user = await _context.Users.FirstOrDefaultAsync(x => x.Id == id);
            if (user == null)
                return NotFound(new { message = "User not found" });

            _context.Users.Remove(user);
            await _context.SaveChangesAsync();

            return Ok(new { message = "User deleted successfully" });
        }

        private async Task<List<Role>> ResolveRolesAsync(List<string> roleNames)
        {
            var normalized = roleNames
                .Where(x => !string.IsNullOrWhiteSpace(x))
                .Select(x => x.Trim())
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList();

            if (normalized.Count == 0)
                return new List<Role>();

            return await _context.Roles
                .Where(x => normalized.Contains(x.Name))
                .ToListAsync();
        }
    }
}