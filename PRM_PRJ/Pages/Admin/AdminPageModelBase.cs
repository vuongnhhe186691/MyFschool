using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace PRM_PRJ.Pages.Admin
{
    public abstract class AdminPageModelBase : PageModel
    {
        protected Guid CurrentAdminUserId { get; private set; }
        protected string CurrentAdminUsername { get; private set; } = string.Empty;

        [BindProperty(SupportsGet = true)]
        public int PageNumber { get; set; } = 1;

        public int PageSize { get; protected set; } = 10;
        public int TotalItems { get; protected set; }
        public int TotalPages { get; protected set; }
        public int StartPage { get; protected set; }
        public int EndPage { get; protected set; }

        protected void ComputePagination(int totalItems, int pageSize)
        {
            PageSize = pageSize;
            TotalItems = totalItems;
            TotalPages = Math.Max(1, (int)Math.Ceiling(totalItems / (double)pageSize));
            if (PageNumber < 1) PageNumber = 1;
            if (PageNumber > TotalPages) PageNumber = TotalPages;

            StartPage = Math.Max(1, PageNumber - 2);
            EndPage = Math.Min(TotalPages, StartPage + 4);
            StartPage = Math.Max(1, EndPage - 4);
        }

        public override void OnPageHandlerExecuting(PageHandlerExecutingContext context)
        {
            var userIdStr = context.HttpContext.Session.GetString("AdminUserId");
            var role = context.HttpContext.Session.GetString("AdminRole");

            if (!Guid.TryParse(userIdStr, out var adminUserId) ||
                !string.Equals(role, "Admin", StringComparison.OrdinalIgnoreCase))
            {
                context.Result = new RedirectToPageResult("/Admin/Login");
                return;
            }

            CurrentAdminUserId = adminUserId;
            CurrentAdminUsername = context.HttpContext.Session.GetString("AdminUsername") ?? "Admin";

            base.OnPageHandlerExecuting(context);
        }
    }
}
