
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using WebProject.Data;
using WebProject.Models;

namespace WebProject.Pages
{
    [Authorize(Roles = "Admin")]
    public class StaffAccountsModel : PageModel
    {
        private readonly ApplicationDbContext _context;

        public StaffAccountsModel(ApplicationDbContext context)
        {
            _context = context;
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

            _context.Staff.Remove(staff);
            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] =
                "Staff account deleted successfully.";

            return RedirectToPage();
        }
    }
}
