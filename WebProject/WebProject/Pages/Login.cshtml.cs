
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using WebProject.Data;
using WebProject.Models;

namespace WebProject.Pages
{
    public class LoginModel : PageModel
    {
        private readonly ApplicationDbContext _context;
        private readonly IPasswordHasher<Staff> _passwordHasher;

        public LoginModel(
            ApplicationDbContext context,
            IPasswordHasher<Staff> passwordHasher)
        {
            _context = context;
            _passwordHasher = passwordHasher;
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
            string email = Email.Trim().ToLowerInvariant();

            // Find staff member by email
            var staff = await _context.Staff
                .FirstOrDefaultAsync(s =>
                    s.StaffEmail.ToLower() == email);

            if (staff == null)
            {
                ErrorMessage = "Invalid email or password.";
                return Page();
            }

            // Verify password against the stored hash
            PasswordVerificationResult result;

            try
            {
                result = _passwordHasher.VerifyHashedPassword(
                    staff,
                    staff.PasswordHash,
                    Password);
            }
            catch (FormatException)
            {
                ErrorMessage = "Invalid email or password.";
                return Page();
            }

            if (result == PasswordVerificationResult.Failed)
            {
                ErrorMessage = "Invalid email or password.";
                return Page();
            }

            // Create authentication claims
            var claims = new List<Claim>
            {
                new Claim(
                    ClaimTypes.NameIdentifier,
                    staff.StaffId.ToString()),

                new Claim(
                    ClaimTypes.Name,
                    staff.StaffEmail),

                new Claim(
                    ClaimTypes.Email,
                    staff.StaffEmail)
            };

            var identity = new ClaimsIdentity(
                claims,
                CookieAuthenticationDefaults.AuthenticationScheme);

            var principal = new ClaimsPrincipal(identity);

            // Sign in using cookie authentication
            await HttpContext.SignInAsync(
                CookieAuthenticationDefaults.AuthenticationScheme,
                principal);

            return RedirectToPage("/Dashboard");
        }
    }
}
