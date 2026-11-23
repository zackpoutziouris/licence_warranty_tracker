
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
    [Authorize(Roles = "Admin,Staff")]
    public class ClientsModel : PageModel
    {
        private readonly ApplicationDbContext _context;

        public ClientsModel(ApplicationDbContext context)
        {
            _context = context;
        }

        [BindProperty(SupportsGet = true)]
        public string? SearchTerm { get; set; }

        public List<ClientSummary> Clients { get; set; } = new();

        public async Task<IActionResult> OnGetAsync()
        {
            var query = _context.Clients
                .AsNoTracking()
                .AsQueryable();

            // Normal staff can only see clients assigned to them.
            if (!User.IsInRole("Admin"))
            {
                string? staffIdValue =
                    User.FindFirstValue(ClaimTypes.NameIdentifier);

                if (!int.TryParse(staffIdValue, out int staffId))
                {
                    return Forbid();
                }

                query = query.Where(c =>
                    c.ClientStaff.Any(cs => cs.StaffId == staffId));
            }

            // Search within the clients the user can access.
            if (!string.IsNullOrWhiteSpace(SearchTerm))
            {
                string search = SearchTerm.Trim();

                query = query.Where(c =>
                    EF.Functions.Like(c.ClientName, $"%{search}%"));
            }

            var clients = await query
                .Include(c => c.Warranties)
                .Include(c => c.Licenses)
                .AsSplitQuery()
                .OrderBy(c => c.ClientName)
                .ToListAsync();

            Clients = clients.Select(client => new ClientSummary
            {
                ClientId = client.ClientId,
                ClientName = client.ClientName,

                Warranties = ExpirySummaryService.Calculate(
                    client.Warranties.Select(w => w.ExpiryDate)),

                Licenses = ExpirySummaryService.Calculate(
                    client.Licenses.Select(l => l.ExpiryDate))

            }).ToList();

            return Page();
        }
    }
}
