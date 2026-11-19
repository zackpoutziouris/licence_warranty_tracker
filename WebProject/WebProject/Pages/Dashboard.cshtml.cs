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
    [Authorize]
    public class DashboardModel : PageModel
    {
        private readonly AuditService _auditService;
        private readonly ApplicationDbContext _context;

        public DashboardModel(
            AuditService auditService,
            ApplicationDbContext context)
        {
            _auditService = auditService;
            _context = context;
        }

        // Overall statistics
        public int TotalClients { get; set; }

        public ExpirySummary TotalWarranties { get; set; } = new();
        public ExpirySummary TotalLicenses { get; set; } = new();

        // Individual client statistics
        public List<ClientSummary> ClientSummaries { get; set; } = new();

        public async Task OnGetAsync()
        {
            // Only admins can see statistics for all clients
            if (User.IsInRole("Admin"))
            {
                await LoadClientStatisticsAsync();
            }
        }

        private async Task LoadClientStatisticsAsync()
        {
            var clients = await _context.Clients
                .AsNoTracking()
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
        }

        public async Task<IActionResult> OnPostLogoutAsync()
        {
            string? staffId =
                User.FindFirstValue(ClaimTypes.NameIdentifier);

            if (staffId != null)
            {
                await _auditService.LogAsync(
                    $"Staff ID {staffId} logged out.");
            }

            await HttpContext.SignOutAsync(
                CookieAuthenticationDefaults.AuthenticationScheme);

            return RedirectToPage("/Login");
        }
    }
}
