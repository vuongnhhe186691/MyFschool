using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PRM_PRJ.DTO.Request;
using PRM_PRJ.DTO.Response;
using PRM_PRJ.Models;

namespace PRM_PRJ.Pages.Admin
{
    public class UsersModel : AdminPageModelBase
    {
        private readonly AppDbContext _context;

        public UsersModel(AppDbContext context)
        {
            _context = context;
        }

        [BindProperty(SupportsGet = true)]
        public string? Search { get; set; }

        [BindProperty(SupportsGet = true)]
        public string? RoleFilter { get; set; }

        [BindProperty]
        public AdminUpsertUserRequest Input { get; set; } = new();

        [BindProperty]
        public Guid EditUserId { get; set; }

        public List<AdminUserItemResponse> Users { get; set; } = new();
        public List<string> AvailableRoles { get; set; } = new();

        public async Task OnGetAsync()
        {
            await LoadDataAsync();
        }

        public async Task<IActionResult> OnPostCreateAsync()
        {
            var normalizedPhone = Input.PhoneNumber?.Trim() ?? string.Empty;
            var normalizedEmail = Input.Email?.Trim().ToLowerInvariant() ?? string.Empty;

            if (string.IsNullOrWhiteSpace(Input.Username) ||
                string.IsNullOrWhiteSpace(normalizedPhone) ||
                string.IsNullOrWhiteSpace(normalizedEmail) ||
                string.IsNullOrWhiteSpace(Input.Password))
            {
                TempData["ErrorMessage"] = "Vui lòng điền đầy đủ Username, Email, Số điện thoại và Mật khẩu.";
                return RedirectToPage();
            }

            if (await _context.Users.AnyAsync(x => x.PhoneNumber == normalizedPhone))
            {
                TempData["ErrorMessage"] = "Số điện thoại đã tồn tại.";
                return RedirectToPage();
            }

            if (await _context.Users.AnyAsync(x => x.Email == normalizedEmail))
            {
                TempData["ErrorMessage"] = "Email đã tồn tại.";
                return RedirectToPage();
            }

            var roles = await ResolveRolesAsync(Input.Roles);
            if (roles.Count == 0)
            {
                TempData["ErrorMessage"] = "Vui lòng chọn ít nhất một Role hợp lệ.";
                return RedirectToPage();
            }

            var user = new User
            {
                Username = Input.Username.Trim(),
                Email = normalizedEmail,
                PhoneNumber = normalizedPhone,
                PasswordHash = BCrypt.Net.BCrypt.HashPassword(Input.Password.Trim()),
                IsActive = Input.IsActive
            };

            _context.Users.Add(user);
            foreach (var role in roles)
            {
                _context.UserRoles.Add(new UserRole { User = user, RoleId = role.Id });
            }

            await _context.SaveChangesAsync();
            TempData["SuccessMessage"] = $"Tạo người dùng '{user.Username}' thành công.";
            return RedirectToPage();
        }

        public async Task<IActionResult> OnPostUpdateAsync()
        {
            var user = await _context.Users
                .Include(x => x.UserRoles)
                .FirstOrDefaultAsync(x => x.Id == EditUserId);

            if (user == null)
            {
                TempData["ErrorMessage"] = "Không tìm thấy người dùng.";
                return RedirectToPage();
            }

            var normalizedPhone = Input.PhoneNumber?.Trim() ?? string.Empty;
            var normalizedEmail = Input.Email?.Trim().ToLowerInvariant() ?? string.Empty;

            if (await _context.Users.AnyAsync(x => x.Id != EditUserId && x.PhoneNumber == normalizedPhone))
            {
                TempData["ErrorMessage"] = "Số điện thoại đã được người dùng khác sử dụng.";
                return RedirectToPage();
            }

            if (await _context.Users.AnyAsync(x => x.Id != EditUserId && x.Email == normalizedEmail))
            {
                TempData["ErrorMessage"] = "Email đã được người dùng khác sử dụng.";
                return RedirectToPage();
            }

            var roles = await ResolveRolesAsync(Input.Roles);
            if (roles.Count == 0)
            {
                TempData["ErrorMessage"] = "Vui lòng chọn ít nhất một Role hợp lệ.";
                return RedirectToPage();
            }

            user.Username = Input.Username.Trim();
            user.Email = normalizedEmail;
            user.PhoneNumber = normalizedPhone;
            user.IsActive = Input.IsActive;

            if (!string.IsNullOrWhiteSpace(Input.Password))
            {
                user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(Input.Password.Trim());
            }

            _context.UserRoles.RemoveRange(user.UserRoles);
            foreach (var role in roles)
            {
                _context.UserRoles.Add(new UserRole { UserId = user.Id, RoleId = role.Id });
            }

            await _context.SaveChangesAsync();
            TempData["SuccessMessage"] = $"Cập nhật người dùng '{user.Username}' thành công.";
            return RedirectToPage();
        }

        public async Task<IActionResult> OnPostDeleteAsync(Guid id)
        {
            var user = await _context.Users.FirstOrDefaultAsync(x => x.Id == id);
            if (user == null)
            {
                TempData["ErrorMessage"] = "Không tìm thấy người dùng.";
                return RedirectToPage();
            }

            if (user.Id == CurrentAdminUserId)
            {
                TempData["ErrorMessage"] = "Không thể xóa chính tài khoản Admin đang đăng nhập.";
                return RedirectToPage();
            }

            try
            {
                _context.Users.Remove(user);
                await _context.SaveChangesAsync();
                TempData["SuccessMessage"] = $"Đã xóa người dùng '{user.Username}'.";
            }
            catch (DbUpdateException)
            {
                TempData["ErrorMessage"] = $"Không thể xóa '{user.Username}' do đang có dữ liệu liên kết (Học sinh, Giáo viên, Đơn từ hoặc Thông báo). Bạn có thể chuyển trạng thái sang Locked.";
            }

            return RedirectToPage();
        }

        private async Task LoadDataAsync()
        {
            AvailableRoles = await _context.Roles
                .OrderBy(x => x.Name)
                .Select(x => x.Name)
                .ToListAsync();

            var query = _context.Users
                .Include(x => x.UserRoles)
                    .ThenInclude(x => x.Role)
                .AsQueryable();

            if (!string.IsNullOrWhiteSpace(Search))
            {
                var term = Search.Trim().ToLower();
                query = query.Where(x =>
                    x.Username.ToLower().Contains(term) ||
                    x.Email.ToLower().Contains(term) ||
                    (x.PhoneNumber != null && x.PhoneNumber.Contains(term)));
            }

            if (!string.IsNullOrWhiteSpace(RoleFilter))
            {
                query = query.Where(x => x.UserRoles.Any(ur => ur.Role.Name == RoleFilter));
            }

            var totalCount = await query.CountAsync();
            ComputePagination(totalCount, 10);

            Users = await query
                .OrderBy(x => x.Username)
                .Skip((PageNumber - 1) * PageSize)
                .Take(PageSize)
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
        }

        private async Task<List<Role>> ResolveRolesAsync(List<string>? roleNames)
        {
            if (roleNames == null || roleNames.Count == 0)
                return new List<Role>();

            var normalized = roleNames
                .Where(x => !string.IsNullOrWhiteSpace(x))
                .Select(x => x.Trim())
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList();

            return await _context.Roles
                .Where(x => normalized.Contains(x.Name))
                .ToListAsync();
        }
    }
}
