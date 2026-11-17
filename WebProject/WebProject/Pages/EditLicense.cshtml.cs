using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;
using System.Security.Claims;
using WebProject.Data;
using WebProject.Helpers;
using WebProject.Services;

namespace WebProject.Pages
{
    [Authorize(Roles = "Admin")]
    public class EditLicenseModel : PageModel
    {
        private readonly ApplicationDbContext _context;
        private readonly AuditService _auditService;

        public EditLicenseModel(
            ApplicationDbContext context,
            AuditService auditService)
        {
            _context = context;
            _auditService = auditService;
        }

        [BindProperty]
        public LicenseInput Input { get; set; } = new();

        public int LicenseId { get; set; }
        public int ClientId { get; set; }

        public string MinimumExpiryDate =>
            UkDateHelper.Today()
                .AddDays(1)
                .ToString("yyyy-MM-dd");

        public class LicenseInput
        {
            [Required]
            public string LicenseName { get; set; } = string.Empty;

            [Required]
            [DataType(DataType.Date)]
            [Display(Name = "License Expiry Date")]
            public DateTime? ExpiryDate { get; set; }
        }

        public async Task<IActionResult> OnGetAsync(int id)
        {
            var license = await _context.Licenses
                .AsNoTracking()
                .FirstOrDefaultAsync(l => l.LicenseId == id);

            if (license == null)
                return NotFound();

            LicenseId = id;
            ClientId = license.ClientId;

            Input = new LicenseInput
            {
                LicenseName = license.LicenseName,
                ExpiryDate = DateTime.ParseExact(
                    license.ExpiryDate,
                    "yyyy-MM-dd",
                    System.Globalization.CultureInfo.InvariantCulture)
            };

            return Page();
        }

        public async Task<IActionResult> OnPostAsync(int id)
        {
            var license = await _context.Licenses
                .FirstOrDefaultAsync(l => l.LicenseId == id);

            if (license == null)
                return NotFound();

            LicenseId = id;
            ClientId = license.ClientId;

            if (Input.ExpiryDate.HasValue &&
                Input.ExpiryDate.Value.Date <= UkDateHelper.Today())
            {
                ModelState.AddModelError(
                    "Input.ExpiryDate",
                    "License Expiry Date must be in the future.");
            }

            if (!ModelState.IsValid)
                return Page();

            string licenseName = Input.LicenseName.Trim();

            if (string.IsNullOrWhiteSpace(licenseName))
            {
                ModelState.AddModelError(
                    "Input.LicenseName",
                    "License name cannot be empty.");

                return Page();
            }

            string expiryDate = Input.ExpiryDate!.Value
                .ToString("yyyy-MM-dd");

            string? currentStaffId =
                User.FindFirstValue(ClaimTypes.NameIdentifier);

            if (license.LicenseName != licenseName)
            {
                _auditService.AddAudit(
                    $"Staff ID {currentStaffId} changed license name for License ID {id} from '{license.LicenseName}' to '{licenseName}'.");

                license.LicenseName = licenseName;
            }

            if (license.ExpiryDate != expiryDate)
            {
                _auditService.AddAudit(
                    $"Staff ID {currentStaffId} changed License Expiry Date for License ID {id} from {license.ExpiryDate} to {expiryDate}.");

                license.ExpiryDate = expiryDate;
            }

            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] =
                "License updated successfully.";

            return RedirectToPage(
                "/ClientDetails",
                new { id = license.ClientId });
        }
    }
}
