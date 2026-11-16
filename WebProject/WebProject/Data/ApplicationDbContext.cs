using Microsoft.EntityFrameworkCore;
using WebProject.Models;

namespace WebProject.Data
{
    public class ApplicationDbContext(
        DbContextOptions<ApplicationDbContext> options)
        : DbContext(options)
    {
        public DbSet<Staff> Staff { get; set; }
        public DbSet<Audit> Audit { get; set; }
    }
}