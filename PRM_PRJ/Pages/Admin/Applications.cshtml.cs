using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PRM_PRJ.DTO.Response;
using PRM_PRJ.Models;

namespace PRM_PRJ.Pages.Admin
{
    public class ApplicationsModel : AdminPageModelBase
    {
        private readonly AppDbContext _context;

        public ApplicationsModel(AppDbContext context)
        {
            _context = context;
        }

        [BindProperty(SupportsGet = true)]
        public string? Search { get; set; }

        public List<ApplicationItemResponse> Applications { get; set; } = new();

        public async Task OnGetAsync()
        {
            var query = _context.Applications
                .Include(x => x.SenderUser)
                .AsQueryable();

            if (!string.IsNullOrWhiteSpace(Search))
            {
                var term = Search.Trim().ToLower();
                query = query.Where(x =>
                    x.Title.ToLower().Contains(term) ||
                    x.Content.ToLower().Contains(term) ||
                    x.SenderUser.Username.ToLower().Contains(term));
            }

            var totalCount = await query.CountAsync();
            ComputePagination(totalCount, 10);

            Applications = await query
                .OrderByDescending(x => x.CreatedAt)
                .Skip((PageNumber - 1) * PageSize)
                .Take(PageSize)
                .Select(x => new ApplicationItemResponse
                {
                    Id = x.Id,
                    Title = x.Title,
                    Content = x.Content,
                    CreatedAt = x.CreatedAt.ToLocalTime().ToString("yyyy-MM-dd HH:mm"),
                    CreatedBy = x.SenderUser.Username
                })
                .ToListAsync();
        }
    }
}
