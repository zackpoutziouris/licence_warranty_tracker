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
    [Authorize(Roles = "Admin")]
    public class AddWarrantyModel : PageModel
    {
        public string MinimumExpiryDate =>
            UkDateHelper.Today()
                .AddDays(1)
                .ToString("yyyy-MM-dd");

        private readonly ApplicationDbContext _context;
        private readonly AuditService _auditService;

        public AddWarrantyModel(
            ApplicationDbContext context,
            AuditService auditService)
        {
            _context = context;
            _auditService = auditService;
        }

        public Client Client { get; set; } = null!;

        [BindProperty]
        public WarrantyInput Input { get; set; } = new();

        public class WarrantyInput
        {
            [Required]
            public string AssetName { get; set; } = string.Empty;

            [Required]
            [DataType(DataType.Date)]
            [Display(Name = "Warranty Expiry Date")]
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
                    "Warranty Expiry Date must be in the future.");
            }

            if (!ModelState.IsValid)
            {
                return Page();
            }

            string assetName = Input.AssetName.Trim();

            if (string.IsNullOrWhiteSpace(assetName))
            {
                ModelState.AddModelError(
                    "Input.AssetName",
                    "Asset name cannot be empty.");

                return Page();
            }

            var warranty = new Warranty
            {
                ClientId = clientId,
                AssetName = assetName,
                ExpiryDate = Input.ExpiryDate!.Value
                    .ToString("yyyy-MM-dd")
            };

            await using var transaction =
                await _context.Database.BeginTransactionAsync();

            _context.Warranties.Add(warranty);
            await _context.SaveChangesAsync();

            string? currentStaffId =
                User.FindFirstValue(ClaimTypes.NameIdentifier);

            _auditService.AddAudit(
                $"Staff ID {currentStaffId} added Warranty ID {warranty.WarrantyId} ({warranty.AssetName}) to Client ID {clientId}, expiring {warranty.ExpiryDate}.");

            await _context.SaveChangesAsync();
            await transaction.CommitAsync();

            TempData["SuccessMessage"] =
                "Warranty added successfully.";

            return RedirectToPage(
                "/ClientDetails",
                new { id = clientId });
        }
    }
}