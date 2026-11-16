using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;
using System.Globalization;
using WebProject.Data;
using WebProject.Models;
using WebProject.Services;
using System.Security.Claims;

namespace WebProject.Pages
{
    [Authorize(Roles = "Admin")]
    public class ExportAuditModel : PageModel
    {
        private readonly ApplicationDbContext _context;
        private readonly AuditPdfService _pdfService;
        private readonly AuditService _auditService;

        public ExportAuditModel(
            ApplicationDbContext context,
            AuditPdfService pdfService,
            AuditService auditService)
        {
            _context = context;
            _pdfService = pdfService;
            _auditService = auditService;
        }

        [BindProperty]
        public ExportInput Input { get; set; } = new();

        public class ExportInput
        {
            public string ExportType { get; set; } = "All";

            [DataType(DataType.Date)]
            public DateTime? StartDate { get; set; }

            [DataType(DataType.Date)]
            public DateTime? EndDate { get; set; }
        }

        public void OnGet()
        {
        }

        public async Task<IActionResult> OnPostAsync()
        {
            if (Input.ExportType != "All" &&
                Input.ExportType != "Custom")
            {
                ModelState.AddModelError(
                    "Input.ExportType",
                    "Select a valid export type.");

                return Page();
            }

            if (Input.ExportType == "Custom")
            {
                if (!Input.StartDate.HasValue ||
                    !Input.EndDate.HasValue)
                {
                    ModelState.AddModelError(
                        string.Empty,
                        "Please select both a start and end date.");

                    return Page();
                }

                if (Input.StartDate.Value.Date >
                    Input.EndDate.Value.Date)
                {
                    ModelState.AddModelError(
                        string.Empty,
                        "Start date cannot be after end date.");

                    return Page();
                }
            }

            // Retrieve all audit records
            var audits = await _context.Audit
                .AsNoTracking()
                .ToListAsync();

            var parsedAudits = new List<(Audit Record, DateTime Date)>();

            foreach (var audit in audits)
            {
                // Existing SQLite format: DD/MM/YY HH:MM
                if (!DateTime.TryParseExact(
                    audit.AuditDateTime,
                    "dd/MM/yy HH:mm",
                    CultureInfo.InvariantCulture,
                    DateTimeStyles.None,
                    out DateTime auditDate))
                {
                    ModelState.AddModelError(
                        string.Empty,
                        $"Audit record {audit.AuditId} has an invalid date.");

                    return Page();
                }

                parsedAudits.Add((audit, auditDate));
            }

            string timeframe = "All Time";

            if (Input.ExportType == "Custom")
            {
                DateTime start = Input.StartDate!.Value.Date;
                DateTime endExclusive =
                    Input.EndDate!.Value.Date.AddDays(1);

                parsedAudits = parsedAudits
                    .Where(a => a.Date >= start &&
                                a.Date < endExclusive)
                    .ToList();

                timeframe =
                    $"{start:dd/MM/yyyy} - {Input.EndDate.Value:dd/MM/yyyy}";
            }

            // Order records chronologically
            var selectedAudits = parsedAudits
                .OrderBy(a => a.Date)
                .ThenBy(a => a.Record.AuditId)
                .Select(a => a.Record)
                .ToList();

            // Generate the PDF
            byte[] pdf = _pdfService.GeneratePdf(
                selectedAudits,
                timeframe);

            // Generate a filename
            string filename =
                $"AuditTrail_{DateTime.UtcNow:yyyyMMdd_HHmmss}.pdf";

            string? staffId =
                User.FindFirstValue(ClaimTypes.NameIdentifier);

            await _auditService.LogAsync(
                $"Staff ID {staffId} exported audit trail for {timeframe}.");

            // Return the PDF as a downloadable file
            return File(
                pdf,
                "application/pdf",
                filename);
        }
    }
}
