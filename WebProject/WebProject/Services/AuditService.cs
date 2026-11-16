
using WebProject.Data;
using WebProject.Models;

namespace WebProject.Services
{
    public class AuditService
    {
        private readonly ApplicationDbContext _context;

        public AuditService(ApplicationDbContext context)
        {
            _context = context;
        }

        public void AddAudit(string message)
        {
            // Get the current UK date and time
            var ukTimeZone =
                TimeZoneInfo.FindSystemTimeZoneById("Europe/London");

            var ukTime = TimeZoneInfo.ConvertTimeFromUtc(
                DateTime.UtcNow,
                ukTimeZone);

            // Format: DD/MM/YY HH:MM
            string formattedDate = ukTime.ToString(
                "dd/MM/yy HH:mm",
                System.Globalization.CultureInfo.InvariantCulture);

            var audit = new Audit
            {
                AuditMessage = message,
                AuditDateTime = formattedDate
            };

            _context.Audit.Add(audit);
        }

        public async Task LogAsync(string message)
        {
            AddAudit(message);
            await _context.SaveChangesAsync();
        }
    }
}
