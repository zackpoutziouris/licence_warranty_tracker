using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;
using System.Security.Claims;
using WebProject.Data;
using WebProject.Models;
using WebProject.Services;

namespace WebProject.Pages
{
    [Authorize(Roles = "Admin")]
    public class CreateAccountModel : PageModel
    {
        private readonly ApplicationDbContext _context;
        private readonly IPasswordHasher<Staff> _passwordHasher;
        private readonly AuditService _auditService;

        public CreateAccountModel(
            ApplicationDbContext context,
            IPasswordHasher<Staff> passwordHasher,
            AuditService auditService)
        {
            _context = context;
            _passwordHasher = passwordHasher;
            _auditService = auditService;
        }

        [BindProperty]
        public AccountInput Input { get; set; } = new();

        public class AccountInput
        {
            [Required]
            [EmailAddress]
            public string Email { get; set; } = string.Empty;

            [Required]
            [MinLength(8, ErrorMessage = "Password must be at least 8 characters.")]
            [DataType(DataType.Password)]
            public string Password { get; set; } = string.Empty;

            [Required]
            [DataType(DataType.Password)]
            [Compare("Password", ErrorMessage = "Passwords do not match.")]
            public string ConfirmPassword { get; set; } = string.Empty;

            public bool IsAdmin { get; set; } = false;
        }

        public void OnGet()
        {
        }

        public async Task<IActionResult> OnPostAsync()
        {
            if (!ModelState.IsValid)
            {
                return Page();
            }

            string email = Input.Email.Trim().ToLowerInvariant();

            // Check whether the email is already registered
            bool emailExists = await _context.Staff
                .AnyAsync(s => s.StaffEmail.ToLower() == email);

            if (emailExists)
            {
                ModelState.AddModelError(
                    "Input.Email",
                    "A staff account with this email already exists.");

                return Page();
            }

            // Create the new staff member
            var staff = new Staff
            {
                StaffEmail = email,
                IsAdmin = Input.IsAdmin
            };

            // Hash the password before saving
            staff.PasswordHash = _passwordHasher.HashPassword(
                staff,
                Input.Password);

            // Add the new staff member to the database
            _context.Staff.Add(staff);

            await _context.SaveChangesAsync();

            string? currentStaffId =
                User.FindFirstValue(ClaimTypes.NameIdentifier);

            string role = staff.IsAdmin ? "Admin" : "Staff";

            await _auditService.LogAsync(
                $"Staff ID {currentStaffId} created Staff ID {staff.StaffId} with role {role}.");

            TempData["SuccessMessage"] = "Staff account created successfully.";

            return RedirectToPage("/Dashboard");
        }
    }
}
