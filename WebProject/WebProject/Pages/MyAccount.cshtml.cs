using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;
using System.Security.Claims;
using WebProject.Data;
using WebProject.Models;

namespace WebProject.Pages
{
    [Authorize(Roles = "Staff")]
    public class MyAccountModel : PageModel
    {
        private readonly ApplicationDbContext _context;
        private readonly IPasswordHasher<Staff> _passwordHasher;

        public MyAccountModel(
            ApplicationDbContext context,
            IPasswordHasher<Staff> passwordHasher)
        {
            _context = context;
            _passwordHasher = passwordHasher;
        }

        public Staff? CurrentStaff { get; set; }

        [BindProperty]
        public PasswordInput Input { get; set; } = new();

        public class PasswordInput
        {
            [Required(ErrorMessage = "Enter your current password.")]
            [DataType(DataType.Password)]
            public string CurrentPassword { get; set; } = string.Empty;

            [Required(ErrorMessage = "Enter a new password.")]
            [MinLength(8, ErrorMessage = "Password must be at least 8 characters.")]
            [DataType(DataType.Password)]
            public string NewPassword { get; set; } = string.Empty;

            [Required(ErrorMessage = "Confirm your new password.")]
            [DataType(DataType.Password)]
            [Compare("NewPassword", ErrorMessage = "New passwords do not match.")]
            public string ConfirmPassword { get; set; } = string.Empty;
        }

        private async Task<Staff?> GetCurrentStaffAsync()
        {
            string? staffId = User.FindFirstValue(
                ClaimTypes.NameIdentifier);

            if (!int.TryParse(staffId, out int id))
            {
                return null;
            }

            return await _context.Staff
                .FirstOrDefaultAsync(s => s.StaffId == id);
        }

        public async Task<IActionResult> OnGetAsync()
        {
            CurrentStaff = await GetCurrentStaffAsync();

            if (CurrentStaff == null)
            {
                return RedirectToPage("/Login");
            }

            return Page();
        }

        public async Task<IActionResult> OnPostAsync()
        {
            CurrentStaff = await GetCurrentStaffAsync();

            if (CurrentStaff == null)
            {
                return RedirectToPage("/Login");
            }

            if (!ModelState.IsValid)
            {
                return Page();
            }

            // Verify the current password
            var result = _passwordHasher.VerifyHashedPassword(
                CurrentStaff,
                CurrentStaff.PasswordHash,
                Input.CurrentPassword);

            if (result == PasswordVerificationResult.Failed)
            {
                ModelState.AddModelError(
                    "Input.CurrentPassword",
                    "Current password is incorrect.");

                return Page();
            }

            // Prevent reusing the same password
            var samePassword = _passwordHasher.VerifyHashedPassword(
                CurrentStaff,
                CurrentStaff.PasswordHash,
                Input.NewPassword);

            if (samePassword != PasswordVerificationResult.Failed)
            {
                ModelState.AddModelError(
                    "Input.NewPassword",
                    "New password must be different from your current password.");

                return Page();
            }

            // Hash the new password
            CurrentStaff.PasswordHash = _passwordHasher.HashPassword(
                CurrentStaff,
                Input.NewPassword);

            // Invalidate all existing login sessions
            CurrentStaff.SessionVersion++;

            await _context.SaveChangesAsync();

            // Expire the current browser's authentication cookie
            await HttpContext.SignOutAsync(
                CookieAuthenticationDefaults.AuthenticationScheme);

            // Allow the confirmation page to display
            TempData["PasswordChanged"] = true;

            // Show confirmation before returning to Login
            return RedirectToPage("/PasswordChanged");
        }
    }
}
