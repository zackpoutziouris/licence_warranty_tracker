using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;
using System.Security.Claims;
using WebProject.Data;
using WebProject.Models;
using WebProject.Services;

namespace WebProject.Pages
{
    [Authorize(Roles = "Admin")]
    public class EditClientModel : PageModel
    {
        private readonly ApplicationDbContext _context;
        private readonly AuditService _auditService;

        public EditClientModel(
            ApplicationDbContext context,
            AuditService auditService)
        {
            _context = context;
            _auditService = auditService;
        }

        [BindProperty]
        public ClientInput Input { get; set; } = new();

        [BindProperty]
        public List<int> SelectedStaffIds { get; set; } = new();

        public List<Staff> AvailableStaff { get; set; } = new();

        public int ClientId { get; set; }

        public class ClientInput
        {
            [Required]
            [StringLength(150)]
            public string ClientName { get; set; } = string.Empty;
        }

        private async Task LoadStaffAsync()
        {
            AvailableStaff = await _context.Staff
                .AsNoTracking()
                .OrderBy(s => s.StaffEmail)
                .ToListAsync();
        }

        public async Task<IActionResult> OnGetAsync(int id)
        {
            var client = await _context.Clients
                .Include(c => c.ClientStaff)
                .FirstOrDefaultAsync(c => c.ClientId == id);

            if (client == null)
                return NotFound();

            ClientId = id;

            Input.ClientName = client.ClientName;

            SelectedStaffIds = client.ClientStaff
                .Select(cs => cs.StaffId)
                .ToList();

            await LoadStaffAsync();

            return Page();
        }

        public async Task<IActionResult> OnPostAsync(int id)
        {
            var client = await _context.Clients
                .Include(c => c.ClientStaff)
                .FirstOrDefaultAsync(c => c.ClientId == id);

            if (client == null)
                return NotFound();

            ClientId = id;
            await LoadStaffAsync();

            if (!ModelState.IsValid)
                return Page();

            string newName = Input.ClientName.Trim();

            if (string.IsNullOrWhiteSpace(newName))
            {
                ModelState.AddModelError(
                    "Input.ClientName",
                    "Client name cannot be empty.");

                return Page();
            }

            bool nameExists = await _context.Clients
                .AnyAsync(c =>
                    c.ClientId != id &&
                    c.ClientName.ToLower() == newName.ToLower());

            if (nameExists)
            {
                ModelState.AddModelError(
                    "Input.ClientName",
                    "Another client already uses this name.");

                return Page();
            }

            var selectedIds = SelectedStaffIds
                .Distinct()
                .ToHashSet();

            var validIds = await _context.Staff
                .Where(s => selectedIds.Contains(s.StaffId))
                .Select(s => s.StaffId)
                .ToListAsync();

            if (validIds.Count != selectedIds.Count)
            {
                ModelState.AddModelError(
                    string.Empty,
                    "One or more selected staff accounts are invalid.");

                return Page();
            }

            var existingIds = client.ClientStaff
                .Select(cs => cs.StaffId)
                .ToHashSet();

            var addedIds = selectedIds.Except(existingIds).ToList();
            var removedIds = existingIds.Except(selectedIds).ToList();

            string? currentStaffId =
                User.FindFirstValue(ClaimTypes.NameIdentifier);

            bool nameChanged = client.ClientName != newName;

            if (nameChanged)
            {
                string oldName = client.ClientName;

                client.ClientName = newName;

                _auditService.AddAudit(
                    $"Staff ID {currentStaffId} changed Client ID {id} name from '{oldName}' to '{newName}'.");
            }

            // Remove old assignments
            foreach (int staffId in removedIds)
            {
                var assignment = client.ClientStaff
                    .First(cs => cs.StaffId == staffId);

                _context.ClientStaff.Remove(assignment);

                _auditService.AddAudit(
                    $"Staff ID {currentStaffId} removed Staff ID {staffId} from Client ID {id}.");
            }

            // Add new assignments
            foreach (int staffId in addedIds)
            {
                _context.ClientStaff.Add(new ClientStaff
                {
                    ClientId = id,
                    StaffId = staffId
                });

                _auditService.AddAudit(
                    $"Staff ID {currentStaffId} assigned Staff ID {staffId} to Client ID {id}.");
            }

            // Save changes and audit entries together
            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] =
                "Client updated successfully.";

            return RedirectToPage(
                "/ClientDetails",
                new { id });
        }
    }
}
