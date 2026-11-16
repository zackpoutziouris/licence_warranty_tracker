using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using WebProject.Services;
using System.Security.Claims;

namespace WebProject.Pages
{
    public class DashboardModel : PageModel
    {
        private readonly AuditService _auditService;

        public DashboardModel(AuditService auditService)
        {
            _auditService = auditService;
        }

        public void OnGet()
        {
        }

        public async Task<IActionResult> OnPostLogoutAsync()
        {
            string? staffId =
                User.FindFirstValue(ClaimTypes.NameIdentifier);

            if (staffId != null)
            {
                await _auditService.LogAsync(
                    $"Staff ID {staffId} logged out.");
            }

            await HttpContext.SignOutAsync(
                CookieAuthenticationDefaults.AuthenticationScheme);

            return RedirectToPage("/Login");
        }
    }
}
