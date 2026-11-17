using Microsoft.EntityFrameworkCore;
using PRM_PRJ.DTO.Response;
using PRM_PRJ.Models;

namespace PRM_PRJ.Pages.Admin
{
    public class IndexModel : AdminPageModelBase
    {
        private readonly AppDbContext _context;

        public IndexModel(AppDbContext context)
        {
            _context = context;
        }

        public int TotalUsers { get; set; }
        public int TotalStudents { get; set; }
        public int TotalTeachers { get; set; }
        public int TotalClasses { get; set; }
        public int TotalSubjects { get; set; }
        public int TotalNotifications { get; set; }
        public int TotalApplications { get; set; }

        public List<ApplicationItemResponse> RecentApplications { get; set; } = new();
        public List<AdminNotificationItemResponse> RecentNotifications { get; set; } = new();

        public async Task OnGetAsync()
        {
            TotalUsers = await _context.Users.CountAsync();
            TotalStudents = await _context.Students.CountAsync();
            TotalTeachers = await _context.Teachers.CountAsync();
            TotalClasses = await _context.Classes.CountAsync();
            TotalSubjects = await _context.Subjects.CountAsync();
            TotalNotifications = await _context.AdminNotifications.CountAsync();
            TotalApplications = await _context.Applications.CountAsync();

            RecentApplications = await _context.Applications
                .Include(x => x.SenderUser)
                .OrderByDescending(x => x.CreatedAt)
                .Take(5)
                .Select(x => new ApplicationItemResponse
                {
                    Id = x.Id,
                    Title = x.Title,
                    Content = x.Content,
                    CreatedAt = x.CreatedAt.ToLocalTime().ToString("yyyy-MM-dd HH:mm"),
                    CreatedBy = x.SenderUser.Username
                })
                .ToListAsync();

            RecentNotifications = await _context.AdminNotifications
                .Include(x => x.CreatedByUser)
                .OrderByDescending(x => x.CreatedAt)
                .Take(5)
                .Select(x => new AdminNotificationItemResponse
                {
                    Id = x.Id,
                    Title = x.Title,
                    Content = x.Content,
                    CreatedAt = x.CreatedAt.ToLocalTime().ToString("yyyy-MM-dd HH:mm"),
                    CreatedBy = x.CreatedByUser.Username
                })
                .ToListAsync();
        }
    }
}
