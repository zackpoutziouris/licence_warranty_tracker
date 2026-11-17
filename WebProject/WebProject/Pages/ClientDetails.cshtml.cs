using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using WebProject.Data;
using WebProject.Models;
using WebProject.Services;

namespace WebProject.Pages
{
    [Authorize(Roles = "Admin")]
    public class ClientDetailsModel : PageModel
    {
        private readonly ApplicationDbContext _context;
        private readonly AuditService _auditService;

        public ClientDetailsModel(
            ApplicationDbContext context,
            AuditService auditService)
        {
            _context = context;
            _auditService = auditService;
        }

        public Client Client { get; set; } = null!;

        // Retrieve client details
        public async Task<IActionResult> OnGetAsync(int id)
        {
            var client = await _context.Clients
                .AsNoTracking()
                .Include(c => c.ClientStaff)
                    .ThenInclude(cs => cs.Staff)
                .Include(c => c.Warranties)
                .Include(c => c.Licenses)
                .AsSplitQuery()
                .FirstOrDefaultAsync(c => c.ClientId == id);

            if (client == null)
            {
                return NotFound();
            }

            Client = client;

            return Page();
        }

        // Delete an individual warranty
        public async Task<IActionResult> OnPostDeleteWarrantyAsync(
            int id,
            int warrantyId)
        {
            var warranty = await _context.Warranties
                .FirstOrDefaultAsync(w =>
                    w.WarrantyId == warrantyId &&
                    w.ClientId == id);

            if (warranty == null)
            {
                return NotFound();
            }

            string? currentStaffId =
                User.FindFirstValue(ClaimTypes.NameIdentifier);

            // Record deletion in audit trail
            _auditService.AddAudit(
                $"Staff ID {currentStaffId} deleted Warranty ID {warranty.WarrantyId} ({warranty.AssetName}) from Client ID {id}. Expiry date was {warranty.ExpiryDate}.");

            // Remove warranty
            _context.Warranties.Remove(warranty);

            // Save deletion and audit entry together
            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] =
                "Warranty deleted successfully.";

            return RedirectToPage(
                "/ClientDetails",
                new { id });
        }

        // Delete an individual licence
        public async Task<IActionResult> OnPostDeleteLicenseAsync(
            int id,
            int licenseId)
        {
            var license = await _context.Licenses
                .FirstOrDefaultAsync(l =>
                    l.LicenseId == licenseId &&
                    l.ClientId == id);

            if (license == null)
            {
                return NotFound();
            }

            string? currentStaffId =
                User.FindFirstValue(ClaimTypes.NameIdentifier);

            // Record deletion in audit trail
            _auditService.AddAudit(
                $"Staff ID {currentStaffId} deleted License ID {license.LicenseId} ({license.LicenseName}) from Client ID {id}. Expiry date was {license.ExpiryDate}.");

            // Remove licence
            _context.Licenses.Remove(license);

            // Save deletion and audit entry together
            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] =
                "Licence deleted successfully.";

            return RedirectToPage(
                "/ClientDetails",
                new { id });
        }
    }
}
