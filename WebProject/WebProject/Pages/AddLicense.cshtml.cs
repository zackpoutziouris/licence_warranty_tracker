using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;
using System.Security.Claims;
using WebProject.Data;
using WebProject.Models;
using WebProject.Services;
using WebProject.Helpers;

namespace WebProject.Pages
{
    [Authorize(Roles = "Admin,Staff")]
    public class AddLicenseModel : PageModel
    {
        public string MinimumExpiryDate =>
            UkDateHelper.Today()
                .AddDays(1)
                .ToString("yyyy-MM-dd");
        private readonly ApplicationDbContext _context;
        private readonly AuditService _auditService;

        public AddLicenseModel(
            ApplicationDbContext context,
            AuditService auditService)
        {
            _context = context;
            _auditService = auditService;
        }

        public Client Client { get; set; } = null!;

        [BindProperty]
        public LicenseInput Input { get; set; } = new();

        public class LicenseInput
        {
            [Required]
            public string LicenseName { get; set; } = string.Empty;

            [Required]
            [DataType(DataType.Date)]
            [Display(Name = "License Expiry Date")]
            public DateTime? ExpiryDate { get; set; }
        }

        public async Task<IActionResult> OnGetAsync(int clientId)
        {
            var client = await _context.Clients
                .AsNoTracking()
                .FirstOrDefaultAsync(c => c.ClientId == clientId);

            if (client == null)
            {
                return NotFound();
            }

            Client = client;
            return Page();
        }

        public async Task<IActionResult> OnPostAsync(int clientId)
        {
            var client = await _context.Clients
                .AsNoTracking()
                .FirstOrDefaultAsync(c => c.ClientId == clientId);

            if (client == null)
            {
                return NotFound();
            }

            Client = client;

            if (Input.ExpiryDate.HasValue && Input.ExpiryDate.Value.Date <= UkDateHelper.Today())
            {
                ModelState.AddModelError(
                    "Input.ExpiryDate",
                    "License Expiry Date must be in the future.");
            }
            if (!ModelState.IsValid)
            {
                return Page();
            }

            string licenseName = Input.LicenseName.Trim();

            if (string.IsNullOrWhiteSpace(licenseName))
            {
                ModelState.AddModelError(
                    "Input.LicenseName",
                    "License name cannot be empty.");

                return Page();
            }

            var license = new License
            {
                ClientId = clientId,
                LicenseName = licenseName,
                ExpiryDate = Input.ExpiryDate!.Value
                    .ToString("yyyy-MM-dd")
            };

            await using var transaction =
                await _context.Database.BeginTransactionAsync();

            _context.Licenses.Add(license);
            await _context.SaveChangesAsync();

            string? currentStaffId =
                User.FindFirstValue(ClaimTypes.NameIdentifier);

            _auditService.AddAudit(
                $"Staff ID {currentStaffId} added License ID {license.LicenseId} ({license.LicenseName}) to Client ID {clientId}, expiring {license.ExpiryDate}.");

            await _context.SaveChangesAsync();
            await transaction.CommitAsync();

            TempData["SuccessMessage"] =
                "License added successfully.";

            return RedirectToPage(
                "/ClientDetails",
                new { id = clientId });
        }
    }
}