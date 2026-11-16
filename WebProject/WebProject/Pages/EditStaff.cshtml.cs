
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

            // Update email and administrator status
            staff.StaffEmail = email;
            staff.IsAdmin = Input.IsAdmin;

            // Only update password when a new one was provided
            if (changingPassword)
            {
                staff.PasswordHash = _passwordHasher.HashPassword(
                    staff,
                    Input.NewPassword!);
            }

            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] =
                "Staff account updated successfully.";

            return RedirectToPage("/StaffAccounts");
        }
    }
}
