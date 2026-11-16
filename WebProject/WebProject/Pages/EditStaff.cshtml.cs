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
    [Authorize(Roles = "Admin")]
    public class EditStaffModel : PageModel
    {
        private readonly ApplicationDbContext _context;
        private readonly IPasswordHasher<Staff> _passwordHasher;

        public EditStaffModel(
            ApplicationDbContext context,
            IPasswordHasher<Staff> passwordHasher)
        {
            _context = context;
            _passwordHasher = passwordHasher;
        }

        [BindProperty]
        public StaffInput Input { get; set; } = new();

        public class StaffInput
        {
            [Required]
            [EmailAddress]
            public string Email { get; set; } = string.Empty;

            [MinLength(8, ErrorMessage = "Password must be at least 8 characters.")]
            [DataType(DataType.Password)]
            public string? NewPassword { get; set; }

            [DataType(DataType.Password)]
            public string? ConfirmPassword { get; set; }

            public bool IsAdmin { get; set; }
        }

        public async Task<IActionResult> OnGetAsync(int id)
        {
            var staff = await _context.Staff.FindAsync(id);

            if (staff == null)
            {
                return NotFound();
            }

            Input = new StaffInput
            {
                Email = staff.StaffEmail,
                IsAdmin = staff.IsAdmin
            };

            return Page();
        }

        public async Task<IActionResult> OnPostAsync(int id)
        {
            var staff = await _context.Staff.FindAsync(id);

            if (staff == null)
            {
                return NotFound();
            }

            // Validate password confirmation if changing password
            bool changingPassword =
                !string.IsNullOrEmpty(Input.NewPassword);

            if (changingPassword &&
                Input.NewPassword != Input.ConfirmPassword)
            {
                ModelState.AddModelError(
                    "Input.ConfirmPassword",
                    "Passwords do not match.");
            }

            if (!changingPassword &&
                !string.IsNullOrEmpty(Input.ConfirmPassword))
            {
                ModelState.AddModelError(
                    "Input.NewPassword",
                    "Enter a new password.");
            }

            // Prevent removing your own administrator permissions
            string? currentId =
                User.FindFirstValue(ClaimTypes.NameIdentifier);

            if (staff.StaffId.ToString() == currentId &&
                !Input.IsAdmin)
            {
                ModelState.AddModelError(
                    "Input.IsAdmin",
                    "You cannot remove your own administrator privileges.");
            }

            if (!ModelState.IsValid)
            {
                return Page();
            }

            string email = Input.Email.Trim().ToLowerInvariant();

            // Check for duplicate email on another account
            bool emailExists = await _context.Staff.AnyAsync(s =>
                s.StaffEmail.ToLower() == email &&
                s.StaffId != id);

            if (emailExists)
            {
                ModelState.AddModelError(
                    "Input.Email",
                    "Another account already uses this email.");

                return Page();
            }

            bool emailChanged = staff.StaffEmail != email;
            bool roleChanged = staff.IsAdmin != Input.IsAdmin;

            // Update account details
            staff.StaffEmail = email;
            staff.IsAdmin = Input.IsAdmin;

            if (changingPassword)
            {
                staff.PasswordHash = _passwordHasher.HashPassword(
                    staff,
                    Input.NewPassword!);
            }

            // Invalidate sessions when authentication details change
            if (changingPassword || emailChanged || roleChanged)
            {
                staff.SessionVersion++;
            }


            await _context.SaveChangesAsync();

            string? currentStaffId =
                User.FindFirstValue(ClaimTypes.NameIdentifier);

            bool editingOwnAccount =
                staff.StaffId.ToString() == currentStaffId;

            // If an admin changes their own password
            if (editingOwnAccount && changingPassword)
            {
                // Expire the current authentication cookie
                await HttpContext.SignOutAsync(
                    CookieAuthenticationDefaults.AuthenticationScheme);

                // Allow the password confirmation page to display
                TempData["PasswordChanged"] = true;

                return RedirectToPage("/PasswordChanged");
            }

            // If an admin changes their own email or permissions
            if (editingOwnAccount && (emailChanged || roleChanged))
            {
                await HttpContext.SignOutAsync(
                    CookieAuthenticationDefaults.AuthenticationScheme);

                return RedirectToPage("/Login");
            }

            TempData["SuccessMessage"] =
                "Staff account updated successfully.";

            return RedirectToPage("/StaffAccounts");
        }
    }
}
