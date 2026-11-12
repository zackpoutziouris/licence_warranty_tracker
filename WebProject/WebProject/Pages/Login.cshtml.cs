
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using WebProject.Data;
using WebProject.Models;

namespace WebProject.Pages
{
    public class LoginModel : PageModel
    {
        private readonly ApplicationDbContext _context;

        public LoginModel(ApplicationDbContext context)
        {
            _context = context;
        }

        [BindProperty]
        public string Email { get; set; } = string.Empty;

        [BindProperty]
        public string Password { get; set; } = string.Empty;

        public string ErrorMessage { get; set; } = string.Empty;

        public void OnGet()
        {
        }

        public async Task<IActionResult> OnPostAsync()
        {
            // Find staff member by email
            var staff = await _context.Staff
                .FirstOrDefaultAsync(s =>
                    s.StaffEmail.ToLower() == Email.Trim().ToLower());

            // Check whether staff member exists
            if (staff == null)
            {
                ErrorMessage = "Invalid email or password.";
                return Page();
            }

            // Compare entered password with database password
            if (staff.PasswordHash != Password)
            {
                ErrorMessage = "Invalid email or password.";
                return Page();
            }

            // Create authentication claims
            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier,
                    staff.StaffId.ToString()),

                new Claim(ClaimTypes.Name,
                    staff.StaffEmail),

                new Claim(ClaimTypes.Email,
                    staff.StaffEmail)
            };

            var identity = new ClaimsIdentity(
                claims,
                CookieAuthenticationDefaults.AuthenticationScheme);

            var principal = new ClaimsPrincipal(identity);

            // Sign the user in
            await HttpContext.SignInAsync(
                CookieAuthenticationDefaults.AuthenticationScheme,
                principal);

            // Redirect to dashboard
            return RedirectToPage("/Dashboard");
        }
    }
}
