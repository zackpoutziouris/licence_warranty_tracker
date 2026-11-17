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
    public class EditWarrantyModel : PageModel
    {
        private readonly ApplicationDbContext _context;
        private readonly AuditService _auditService;

        public EditWarrantyModel(
            ApplicationDbContext context,
            AuditService auditService)
        {
            _context = context;
            _auditService = auditService;
        }

        [BindProperty]
        public WarrantyInput Input { get; set; } = new();

        public int WarrantyId { get; set; }
        public int ClientId { get; set; }

        public string MinimumExpiryDate =>
            UkDateHelper.Today()
                .AddDays(1)
                .ToString("yyyy-MM-dd");

        public class WarrantyInput
        {
            [Required]
            public string AssetName { get; set; } = string.Empty;

            [Required]
            [DataType(DataType.Date)]
            [Display(Name = "Warranty Expiry Date")]
            public DateTime? ExpiryDate { get; set; }
        }

        public async Task<IActionResult> OnGetAsync(int id)
        {
            var warranty = await _context.Warranties
                .AsNoTracking()
                .FirstOrDefaultAsync(w => w.WarrantyId == id);

            if (warranty == null)
                return NotFound();

            WarrantyId = id;
            ClientId = warranty.ClientId;

            Input = new WarrantyInput
            {
                AssetName = warranty.AssetName,
                ExpiryDate = DateTime.ParseExact(
                    warranty.ExpiryDate,
                    "yyyy-MM-dd",
                    System.Globalization.CultureInfo.InvariantCulture)
            };

            return Page();
        }

        public async Task<IActionResult> OnPostAsync(int id)
        {
            var warranty = await _context.Warranties
                .FirstOrDefaultAsync(w => w.WarrantyId == id);

            if (warranty == null)
                return NotFound();

            WarrantyId = id;
            ClientId = warranty.ClientId;

            if (Input.ExpiryDate.HasValue &&
                Input.ExpiryDate.Value.Date <= UkDateHelper.Today())
            {
                ModelState.AddModelError(
                    "Input.ExpiryDate",
                    "Warranty Expiry Date must be in the future.");
            }

            if (!ModelState.IsValid)
                return Page();

            string assetName = Input.AssetName.Trim();

            if (string.IsNullOrWhiteSpace(assetName))
            {
                ModelState.AddModelError(
                    "Input.AssetName",
                    "Asset name cannot be empty.");

                return Page();
            }

            string expiryDate = Input.ExpiryDate!.Value
                .ToString("yyyy-MM-dd");

            string? currentStaffId =
                User.FindFirstValue(ClaimTypes.NameIdentifier);

            if (warranty.AssetName != assetName)
            {
                _auditService.AddAudit(
                    $"Staff ID {currentStaffId} changed asset name for Warranty ID {id} from '{warranty.AssetName}' to '{assetName}'.");

                warranty.AssetName = assetName;
            }

            if (warranty.ExpiryDate != expiryDate)
            {
                _auditService.AddAudit(
                    $"Staff ID {currentStaffId} changed Warranty Expiry Date for Warranty ID {id} from {warranty.ExpiryDate} to {expiryDate}.");

                warranty.ExpiryDate = expiryDate;
            }

            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] =
                "Warranty updated successfully.";

            return RedirectToPage(
                "/ClientDetails",
                new { id = warranty.ClientId });
        }
    }
}
