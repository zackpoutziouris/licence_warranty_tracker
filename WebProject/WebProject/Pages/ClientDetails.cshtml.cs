using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using WebProject.Data;
using WebProject.Models;

namespace WebProject.Pages
{
    [Authorize(Roles = "Admin")]
    public class ClientDetailsModel : PageModel
    {
        private readonly ApplicationDbContext _context;

        public ClientDetailsModel(ApplicationDbContext context)
        {
            _context = context;
        }

        public Client Client { get; set; } = null!;

        public async Task<IActionResult> OnGetAsync(int id)
        {
            var client = await _context.Clients
                .AsNoTracking()
                .Include(c => c.ClientStaff)
                    .ThenInclude(cs => cs.Staff)
                .Include(c => c.Warranties)
                .Include(c => c.Licenses)
                .AsSplitQuery()
                .FirstOrDefaultAsync(c => c.ClientId == id);

            if (client == null)
            {
                return NotFound();
            }

            Client = client;

            return Page();
        }
    }
}