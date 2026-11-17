using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using PRM_PRJ.Models;

namespace PRM_PRJ.Pages.Admin
{
    public class LoginModel : PageModel
    {
        private readonly AppDbContext _context;

        public LoginModel(AppDbContext context)
        {
            _context = context;
        }

        [BindProperty]
        public string PhoneNumber { get; set; } = string.Empty;

        [BindProperty]
        public string Password { get; set; } = string.Empty;

        public string? ErrorMessage { get; set; }

        public IActionResult OnGet()
        {
            if (!string.IsNullOrWhiteSpace(HttpContext.Session.GetString("AdminUserId")))
            {
                return RedirectToPage("/Admin/Index");
            }

            return Page();
        }

        public async Task<IActionResult> OnPostAsync()
        {
            var normalizedPhone = PhoneNumber?.Trim() ?? string.Empty;
            if (string.IsNullOrWhiteSpace(normalizedPhone) || string.IsNullOrWhiteSpace(Password))
            {
                ErrorMessage = "Vui lòng nhập đầy đủ số điện thoại và mật khẩu.";
                return Page();
            }

            var user = await _context.Users
                .Include(u => u.UserRoles)
                    .ThenInclude(ur => ur.Role)
                .FirstOrDefaultAsync(x => x.PhoneNumber == normalizedPhone);

            if (user == null || !BCrypt.Net.BCrypt.Verify(Password, user.PasswordHash))
            {
                ErrorMessage = "Sai số điện thoại hoặc mật khẩu.";
                return Page();
            }

            if (!user.IsActive)
            {
                ErrorMessage = "Tài khoản đã bị khóa.";
                return Page();
            }

            var roles = user.UserRoles.Select(x => x.Role.Name).ToList();
            if (!roles.Any(r => string.Equals(r, "Admin", StringComparison.OrdinalIgnoreCase)))
            {
                ErrorMessage = "Chỉ tài khoản có quyền Admin mới được truy cập cổng quản trị Web.";
                return Page();
            }

            HttpContext.Session.SetString("AdminUserId", user.Id.ToString());
            HttpContext.Session.SetString("AdminUsername", user.Username);
            HttpContext.Session.SetString("AdminPhone", user.PhoneNumber ?? string.Empty);
            HttpContext.Session.SetString("AdminRole", "Admin");

            return RedirectToPage("/Admin/Index");
        }
    }
}
