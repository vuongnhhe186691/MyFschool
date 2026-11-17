using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PRM_PRJ.DTO.Request;
using PRM_PRJ.Models;

namespace PRM_PRJ.Pages.Admin
{
    public class NotificationsModel : AdminPageModelBase
    {
        private readonly AppDbContext _context;

        public NotificationsModel(AppDbContext context)
        {
            _context = context;
        }

        [BindProperty]
        public CreateAdminNotificationRequest Input { get; set; } = new();

        public List<NotificationViewItem> Notifications { get; set; } = new();

        public async Task OnGetAsync()
        {
            await LoadDataAsync();
        }

        public async Task<IActionResult> OnPostCreateAsync()
        {
            if (string.IsNullOrWhiteSpace(Input.Title) || string.IsNullOrWhiteSpace(Input.Content))
            {
                TempData["ErrorMessage"] = "Vui lòng nhập đầy đủ Tiêu đề và Nội dung.";
                return RedirectToPage();
            }

            _context.AdminNotifications.Add(new AdminNotification
            {
                Title = Input.Title.Trim(),
                Content = Input.Content.Trim(),
                CreatedByUserId = CurrentAdminUserId,
                CreatedAt = DateTime.UtcNow
            });

            await _context.SaveChangesAsync();
            TempData["SuccessMessage"] = "Gửi thông báo toàn trường thành công.";
            return RedirectToPage();
        }

        private async Task LoadDataAsync()
        {
            var query = _context.AdminNotifications
                .Include(x => x.CreatedByUser)
                .Include(x => x.UserReads)
                .AsQueryable();

            var totalCount = await query.CountAsync();
            ComputePagination(totalCount, 8);

            Notifications = await query
                .OrderByDescending(x => x.CreatedAt)
                .Skip((PageNumber - 1) * PageSize)
                .Take(PageSize)
                .Select(x => new NotificationViewItem
                {
                    Id = x.Id,
                    Title = x.Title,
                    Content = x.Content,
                    CreatedAt = x.CreatedAt.ToLocalTime().ToString("yyyy-MM-dd HH:mm"),
                    CreatedBy = x.CreatedByUser.Username,
                    ReadCount = x.UserReads.Count
                })
                .ToListAsync();
        }

        public class NotificationViewItem
        {
            public int Id { get; set; }
            public string Title { get; set; } = string.Empty;
            public string Content { get; set; } = string.Empty;
            public string CreatedAt { get; set; } = string.Empty;
            public string CreatedBy { get; set; } = string.Empty;
            public int ReadCount { get; set; }
        }
    }
}
