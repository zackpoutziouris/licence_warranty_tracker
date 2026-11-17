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
    public class CreateClientModel : PageModel
    {
        private readonly ApplicationDbContext _context;
        private readonly AuditService _auditService;

        public CreateClientModel(
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

        public class ClientInput
        {
            [Required]
            [StringLength(150)]
            public string ClientName { get; set; } = string.Empty;
        }

        public async Task OnGetAsync()
        {
            await LoadStaffAsync();
        }

        private async Task LoadStaffAsync()
        {
            AvailableStaff = await _context.Staff
                .AsNoTracking()
                .OrderBy(s => s.StaffEmail)
                .ToListAsync();
        }

        public async Task<IActionResult> OnPostAsync()
        {
            await LoadStaffAsync();

            if (!ModelState.IsValid)
            {
                return Page();
            }

            string clientName = Input.ClientName.Trim();

            if (string.IsNullOrWhiteSpace(clientName))
            {
                ModelState.AddModelError(
                    "Input.ClientName",
                    "Client name cannot be empty.");

                return Page();
            }

            bool clientExists = await _context.Clients
                .AnyAsync(c =>
                    c.ClientName.ToLower() ==
                    clientName.ToLower());

            if (clientExists)
            {
                ModelState.AddModelError(
                    "Input.ClientName",
                    "A client with this name already exists.");

                return Page();
            }

            // Remove duplicate selections
            var selectedIds = SelectedStaffIds
                .Distinct()
                .ToList();

            // Ensure all selected staff IDs exist
            int validStaffCount = await _context.Staff
                .CountAsync(s => selectedIds.Contains(s.StaffId));

            if (validStaffCount != selectedIds.Count)
            {
                ModelState.AddModelError(
                    string.Empty,
                    "One or more selected staff accounts are invalid.");

                return Page();
            }

            var client = new Client
            {
                ClientName = clientName
            };

            // Assign selected staff members
            foreach (int staffId in selectedIds)
            {
                client.ClientStaff.Add(new ClientStaff
                {
                    StaffId = staffId
                });
            }

            await using var transaction =
                await _context.Database.BeginTransactionAsync();

            _context.Clients.Add(client);
            await _context.SaveChangesAsync();

            string? currentStaffId =
                User.FindFirstValue(ClaimTypes.NameIdentifier);

            _auditService.AddAudit(
                $"Staff ID {currentStaffId} created Client ID {client.ClientId} ({client.ClientName}).");

            foreach (int staffId in selectedIds)
            {
                _auditService.AddAudit(
                    $"Staff ID {currentStaffId} assigned Staff ID {staffId} to Client ID {client.ClientId}.");
            }

            await _context.SaveChangesAsync();
            await transaction.CommitAsync();

            TempData["SuccessMessage"] =
                "Client created successfully.";

            return RedirectToPage(
                "/ClientDetails",
                new { id = client.ClientId });
        }
    }
}