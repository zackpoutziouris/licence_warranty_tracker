using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using WebProject.Data;
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

        public List<Client> Clients { get; set; } = new();

        public async Task OnGetAsync()
        {
            Clients = await _context.Clients
                .AsNoTracking()
                .OrderBy(c => c.ClientName)
                .ToListAsync();
        }
    }
}