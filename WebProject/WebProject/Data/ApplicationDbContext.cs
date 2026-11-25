using Microsoft.EntityFrameworkCore;
using WebProject.Models;

namespace WebProject.Data
{
    public class ApplicationDbContext : DbContext
    {
        public ApplicationDbContext(
            DbContextOptions<ApplicationDbContext> options)
            : base(options)
        {
        }

        // Tables
        public DbSet<Staff> Staff { get; set; }
        public DbSet<Audit> Audit { get; set; }
        public DbSet<Client> Clients { get; set; }
        public DbSet<Warranty> Warranties { get; set; }
        public DbSet<License> Licenses { get; set; }
        public DbSet<ClientStaff> ClientStaff { get; set; }
        public DbSet<Notification> Notifications { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // Composite primary key for ClientStaff
            modelBuilder.Entity<ClientStaff>()
                .HasKey(cs => new
                {
                    cs.ClientId,
                    cs.StaffId
                });

            // ClientStaff -> Client relationship
            modelBuilder.Entity<ClientStaff>()
                .HasOne(cs => cs.Client)
                .WithMany(c => c.ClientStaff)
                .HasForeignKey(cs => cs.ClientId)
                .OnDelete(DeleteBehavior.Cascade);

            // ClientStaff -> Staff relationship
            modelBuilder.Entity<ClientStaff>()
                .HasOne(cs => cs.Staff)
                .WithMany(s => s.ClientStaff)
                .HasForeignKey(cs => cs.StaffId)
                .OnDelete(DeleteBehavior.Cascade);

            // Warranty -> Client relationship
            modelBuilder.Entity<Warranty>()
                .HasOne(w => w.Client)
                .WithMany(c => c.Warranties)
                .HasForeignKey(w => w.ClientId)
                .OnDelete(DeleteBehavior.NoAction);

            // License -> Client relationship
            modelBuilder.Entity<License>()
                .HasOne(l => l.Client)
                .WithMany(c => c.Licenses)
                .HasForeignKey(l => l.ClientId)
                .OnDelete(DeleteBehavior.NoAction);

            // Notification to Staff relationship
            modelBuilder.Entity<Notification>()
                .HasOne(n => n.Staff)
                .WithMany()
                .HasForeignKey(n => n.AssignedStaff)
                .OnDelete(DeleteBehavior.Cascade);

            // Prevent duplicate notifications
            modelBuilder.Entity<Notification>()
                .HasIndex(n => new
                {
                    n.AssignedStaff,
                    n.ItemType,
                    n.ItemId,
                    n.ExpiryDate,
                    n.NotificationType
                })
                .IsUnique()
                .HasDatabaseName("IX_Notifications_Unique");

            // Default notification status
            modelBuilder.Entity<Notification>()
                .Property(n => n.HasBeenEmailed)
                .HasDefaultValue(false);

            modelBuilder.Entity<Notification>()
                .Property(n => n.HasBeenCleared)
                .HasDefaultValue(false);
        }
    }
}