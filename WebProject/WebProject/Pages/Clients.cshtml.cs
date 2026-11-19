using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using System.Globalization;
using WebProject.Data;
using WebProject.Helpers;
using WebProject.Models;

namespace WebProject.Pages
{
    [Authorize(Roles = "Admin")]
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

        public class ExpirySummary
        {
            public int Total { get; set; }
            public int Days30Plus { get; set; }
            public int Days7To29 { get; set; }
            public int Under7Days { get; set; }
            public int Expired { get; set; }
        }

        public class ClientSummary
        {
            public int ClientId { get; set; }
            public string ClientName { get; set; } = string.Empty;

            public ExpirySummary Warranties { get; set; } = new();
            public ExpirySummary Licenses { get; set; } = new();
        }

        public async Task OnGetAsync()
        {
            var query = _context.Clients
                .AsNoTracking()
                .AsQueryable();

            // Filter clients by name
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

            DateTime today = UkDateHelper.Today();

            foreach (var client in clients)
            {
                Clients.Add(new ClientSummary
                {
                    ClientId = client.ClientId,
                    ClientName = client.ClientName,

                    Warranties = CalculateExpirySummary(
                        client.Warranties.Select(w => w.ExpiryDate),
                        today),

                    Licenses = CalculateExpirySummary(
                        client.Licenses.Select(l => l.ExpiryDate),
                        today)
                });
            }
        }

        private static ExpirySummary CalculateExpirySummary(
            IEnumerable<string> expiryDates,
            DateTime today)
        {
            var summary = new ExpirySummary();

            foreach (string expiryDate in expiryDates)
            {
                summary.Total++;

                if (!DateTime.TryParseExact(
                    expiryDate,
                    "yyyy-MM-dd",
                    CultureInfo.InvariantCulture,
                    DateTimeStyles.None,
                    out DateTime date))
                {
                    // Invalid stored dates are not classified.
                    continue;
                }

                int daysRemaining = (date.Date - today.Date).Days;

                if (daysRemaining < 0)
                {
                    summary.Expired++;
                }
                else if (daysRemaining < 7)
                {
                    summary.Under7Days++;
                }
                else if (daysRemaining < 30)
                {
                    summary.Days7To29++;
                }
                else
                {
                    summary.Days30Plus++;
                }
            }

            return summary;
        }
    }
}
