using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace PRM_PRJ.Pages
{
    public class IndexModel : PageModel
    {
        public IActionResult OnGet()
        {
            var adminId = HttpContext.Session.GetString("AdminUserId");
            if (string.IsNullOrWhiteSpace(adminId))
            {
                return RedirectToPage("/Admin/Login");
            }

            return RedirectToPage("/Admin/Index");
        }
    }
}
