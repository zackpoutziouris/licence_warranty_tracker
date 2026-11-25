using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using WebProject.Data;
using WebProject.Models;
using WebProject.Services;

namespace WebProject.Pages
{
    [Authorize(Roles = "Admin,Staff")]
    public class DashboardModel : PageModel
    {
        private readonly AuditService _auditService;
        private readonly ApplicationDbContext _context;

        public DashboardModel(AuditService auditService, ApplicationDbContext context)
        {
            _auditService = auditService;
            _context = context;
        }

        public int TotalClients { get; set; }
        public ExpirySummary TotalWarranties { get; set; } = new();
        public ExpirySummary TotalLicenses { get; set; } = new();
        public List<ClientSummary> ClientSummaries { get; set; } = new();
        
        // Notification properties
        public List<Notification> Notifications { get; set; } = new();
        public int NotificationCount => Notifications.Count;

        public async Task<IActionResult> OnGetAsync()
        {
            var query = _context.Clients.AsNoTracking().AsQueryable();

            // Admins see all clients. Staff see only clients assigned to them.
            if (!User.IsInRole("Admin"))
            {
                string? staffIdValue = User.FindFirstValue(ClaimTypes.NameIdentifier);
                if (!int.TryParse(staffIdValue, out int staffId))
                {
                    return Forbid();
                }

                query = query.Where(c =>
                    c.ClientStaff.Any(cs => cs.StaffId == staffId));
            }

            var clients = await query
                .Include(c => c.Warranties)
                .Include(c => c.Licenses)
                .AsSplitQuery()
                .OrderBy(c => c.ClientName)
                .ToListAsync();

            TotalClients = clients.Count;

            ClientSummaries = clients.Select(client => new ClientSummary
            {
                ClientId = client.ClientId,
                ClientName = client.ClientName,
                Warranties = ExpirySummaryService.Calculate(
                    client.Warranties.Select(w => w.ExpiryDate)),
                Licenses = ExpirySummaryService.Calculate(
                    client.Licenses.Select(l => l.ExpiryDate))
            }).ToList();

            TotalWarranties = ExpirySummaryService.Combine(
                ClientSummaries.Select(c => c.Warranties));
            TotalLicenses = ExpirySummaryService.Combine(
                ClientSummaries.Select(c => c.Licenses));

            await LoadNotificationsAsync();
            return Page();
        }

        public async Task<IActionResult> OnPostLogoutAsync()
        {
            string? staffId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (staffId != null)
            {
                await _auditService.LogAsync($"Staff ID {staffId} logged out.");
            }

            await HttpContext.SignOutAsync(
                CookieAuthenticationDefaults.AuthenticationScheme);
            return RedirectToPage("/Login");
        }

        private async Task LoadNotificationsAsync()
        {
            string? staffIdValue =
                User.FindFirstValue(ClaimTypes.NameIdentifier);

            if (!int.TryParse(staffIdValue, out int staffId))
            {
                return;
            }

            Notifications = await _context.Notifications
                .AsNoTracking()
                .Where(n =>
                    n.AssignedStaff == staffId &&
                    !n.HasBeenCleared)
                .OrderByDescending(n => n.NotificationDate)
                .ThenByDescending(n => n.NotificationId)
                .ToListAsync();
        }

        public async Task<IActionResult> OnPostClearNotificationsAsync()
        {
            // Retrieve the logged-in staff member's ID
            string? staffIdValue =
                User.FindFirstValue(ClaimTypes.NameIdentifier);

            if (!int.TryParse(staffIdValue, out int staffId))
            {
                return Forbid();
            }

            // Retrieve uncleared notifications belonging to this staff member
            var notifications = await _context.Notifications
                .Where(n =>
                    n.AssignedStaff == staffId &&
                    !n.HasBeenCleared)
                .ToListAsync();

            // Mark each notification as cleared
            foreach (var notification in notifications)
            {
                notification.HasBeenCleared = true;
            }

            // Save changes to the database
            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] =
                "All notifications have been cleared successfully.";

            return RedirectToPage("/Dashboard");
        }
    }
}
