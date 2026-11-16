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
    public class StaffAccountsModel : PageModel
    {
        private readonly ApplicationDbContext _context;
        private readonly AuditService _auditService;    

        public StaffAccountsModel(ApplicationDbContext context, AuditService auditService)
        {
            _context = context;
            _auditService = auditService;
        }

        public List<Staff> StaffAccounts { get; set; } = new();

        public int CurrentStaffId
        {
            get
            {
                int.TryParse(
                    User.FindFirstValue(ClaimTypes.NameIdentifier),
                    out int id);

                return id;
            }
        }

        public async Task OnGetAsync()
        {
            StaffAccounts = await _context.Staff
                .AsNoTracking()
                .OrderBy(s => s.StaffId)
                .ToListAsync();
        }

        public async Task<IActionResult> OnPostDeleteAsync(int id)
        {
            // Prevent an administrator from deleting their own account
            if (id == CurrentStaffId)
            {
                TempData["ErrorMessage"] =
                    "You cannot delete your own account.";

                return RedirectToPage();
            }

            var staff = await _context.Staff.FindAsync(id);

            if (staff == null)
            {
                return NotFound();
            }

            string? currentStaffId =
                User.FindFirstValue(ClaimTypes.NameIdentifier);

            // Add audit entry before deleting the account
            _auditService.AddAudit(
                $"Staff ID {currentStaffId} deleted Staff ID {staff.StaffId}.");

            // Delete the account
            _context.Staff.Remove(staff);

            // Save both operations together
            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] =
                "Staff account deleted successfully.";

            return RedirectToPage();
        }
    }
}
