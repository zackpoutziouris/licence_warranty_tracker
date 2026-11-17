using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using WebProject.Data;
using WebProject.Services;

namespace WebProject.Pages
{
    [Authorize(Roles = "Admin")]
    public class DeleteClientModel : PageModel
    {
        private readonly ApplicationDbContext _context;
        private readonly AuditService _auditService;

        public DeleteClientModel(
            ApplicationDbContext context,
            AuditService auditService)
        {
            _context = context;
            _auditService = auditService;
        }

        public int ClientId { get; set; }
        public string ClientName { get; set; } = string.Empty;
        public int WarrantyCount { get; set; }
        public int LicenseCount { get; set; }
        public int StaffAssignmentCount { get; set; }

        public async Task<IActionResult> OnGetAsync(int id)
        {
            var client = await _context.Clients
                .AsNoTracking()
                .FirstOrDefaultAsync(c => c.ClientId == id);

            if (client == null)
                return NotFound();

            ClientId = client.ClientId;
            ClientName = client.ClientName;

            WarrantyCount = await _context.Warranties
                .CountAsync(w => w.ClientId == id);

            LicenseCount = await _context.Licenses
                .CountAsync(l => l.ClientId == id);

            StaffAssignmentCount = await _context.ClientStaff
                .CountAsync(cs => cs.ClientId == id);

            return Page();
        }

        public async Task<IActionResult> OnPostAsync(int id)
        {
            await using var transaction =
                await _context.Database.BeginTransactionAsync();

            var client = await _context.Clients
                .FirstOrDefaultAsync(c => c.ClientId == id);

            if (client == null)
                return NotFound();

            var warranties = await _context.Warranties
                .Where(w => w.ClientId == id)
                .ToListAsync();

            var licenses = await _context.Licenses
                .Where(l => l.ClientId == id)
                .ToListAsync();

            var assignments = await _context.ClientStaff
                .Where(cs => cs.ClientId == id)
                .ToListAsync();

            string? currentStaffId =
                User.FindFirstValue(ClaimTypes.NameIdentifier);

            // Audit each warranty being removed
            foreach (var warranty in warranties)
            {
                _auditService.AddAudit(
                    $"Staff ID {currentStaffId} deleted Warranty ID {warranty.WarrantyId} ({warranty.AssetName}) from Client ID {id} as part of client deletion.");
            }

            // Audit each licence being removed
            foreach (var license in licenses)
            {
                _auditService.AddAudit(
                    $"Staff ID {currentStaffId} deleted License ID {license.LicenseId} ({license.LicenseName}) from Client ID {id} as part of client deletion.");
            }

            // Audit removed staff assignments
            foreach (var assignment in assignments)
            {
                _auditService.AddAudit(
                    $"Staff ID {currentStaffId} removed Staff ID {assignment.StaffId} from Client ID {id} as part of client deletion.");
            }

            // Delete dependent records first
            _context.Warranties.RemoveRange(warranties);
            _context.Licenses.RemoveRange(licenses);
            _context.ClientStaff.RemoveRange(assignments);

            // Save dependent deletions before deleting the client
            await _context.SaveChangesAsync();

            string clientName = client.ClientName;

            _context.Clients.Remove(client);

            _auditService.AddAudit(
                $"Staff ID {currentStaffId} deleted Client ID {id} ({clientName}).");

            await _context.SaveChangesAsync();

            await transaction.CommitAsync();

            TempData["SuccessMessage"] =
                $"Client '{clientName}' and its associated records were deleted successfully.";

            return RedirectToPage("/Clients");
        }
    }
}
