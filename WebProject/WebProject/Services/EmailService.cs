using MailKit;
using MailKit.Net.Smtp;
using MailKit.Security;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using MimeKit;
using WebProject.Data;
using WebProject.Models;

namespace WebProject.Services
{
    public class EmailService
    {
        private readonly ApplicationDbContext _context;
        private readonly EmailSettings _emailSettings;
        private readonly ILogger<EmailService> _logger;

        // Temporary delay between emails as we are using the free tier of Mailtrap.
        private static readonly TimeSpan EmailSendDelay = TimeSpan.FromSeconds(10);

        public EmailService(
            ApplicationDbContext context,
            IOptions<EmailSettings> emailSettings,
            ILogger<EmailService> logger)
        {
            _context = context;
            _emailSettings = emailSettings.Value;
            _logger = logger;
        }

        public async Task<int> SendPendingNotificationsAsync(
            CancellationToken cancellationToken = default)
        {
            // Retrieve notifications that have not been emailed.
            var notifications = await _context.Notifications
                .Include(n => n.Staff)
                .Where(n =>
                    !n.HasBeenEmailed &&
                    _context.ClientStaff.Any(cs =>
                        cs.ClientId == n.ClientId &&
                        cs.StaffId == n.AssignedStaff))
                .OrderBy(n => n.NotificationId)
                .ToListAsync(cancellationToken);

            if (notifications.Count == 0)
            {
                _logger.LogInformation(
                    "No pending email notifications.");

                return 0;
            }

            int emailsSent = 0;

            using var smtpClient = new SmtpClient();

            try
            {
                // Connect to the SMTP server.
                await smtpClient.ConnectAsync(
                    _emailSettings.Host,
                    _emailSettings.Port,
                    SecureSocketOptions.StartTls,
                    cancellationToken);

                // Authenticate with the SMTP server.
                await smtpClient.AuthenticateAsync(
                    _emailSettings.Username,
                    _emailSettings.Password,
                    cancellationToken);

                /*
                foreach (var notification in notifications)
                {
                    cancellationToken.ThrowIfCancellationRequested();
                */
                
                // Temporary delay between emails:
                bool firstEmail = true;

                foreach (var notification in notifications)
                {
                    cancellationToken.ThrowIfCancellationRequested();

                    // Prevent exceeding the SMTP provider's rate limit.
                    if (!firstEmail)
                    {
                        await Task.Delay(
                            EmailSendDelay,
                            cancellationToken);
                    }

                    firstEmail = false;

                 // Temporary delay between emails ^

                    // Recheck the current client assignment.
                    bool isAssigned = await _context.ClientStaff
                        .AnyAsync(cs =>
                            cs.ClientId == notification.ClientId &&
                            cs.StaffId == notification.AssignedStaff,
                            cancellationToken);

                    if (!isAssigned)
                    {
                        continue;
                    }

                    try
                    {
                        var email = new MimeMessage();

                        email.From.Add(new MailboxAddress(
                            _emailSettings.SenderName,
                            _emailSettings.SenderEmail));

                        email.To.Add(MailboxAddress.Parse(
                            notification.Staff.StaffEmail));

                        email.Subject = GenerateSubject(
                            notification);

                        email.Body = new TextPart("plain")
                        {
                            Text = GenerateEmailBody(notification)
                        };

                        // Send the email.
                        await smtpClient.SendAsync(
                            email,
                            cancellationToken);
                    }
                    catch (OperationCanceledException)
                        when (cancellationToken.IsCancellationRequested)
                    {
                        throw;
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(
                            ex,
                            "Failed to send email for notification ID {Id}.",
                            notification.NotificationId);

                        if (!smtpClient.IsConnected)
                        {
                            break;
                        }

                        continue;
                    }

                    // Only mark as emailed after SMTP accepts it.
                    notification.HasBeenEmailed = true;

                    await _context.SaveChangesAsync(
                        cancellationToken);

                    emailsSent++;

                    _logger.LogInformation(
                        "Email sent for notification ID {Id}.",
                        notification.NotificationId);
                }
            }
            finally
            {
                if (smtpClient.IsConnected)
                {
                    await smtpClient.DisconnectAsync(
                        true,
                        CancellationToken.None);
                }
            }

            _logger.LogInformation(
                "Email processing completed. {Count} emails sent.",
                emailsSent);

            return emailsSent;
        }

        private static string GenerateSubject(
            Notification notification)
        {
            string itemType =
                notification.ItemType == "License"
                    ? "Licence"
                    : "Warranty";

            if (notification.NotificationType.StartsWith("Overdue_"))
            {
                return $"Expired {itemType} Alert";
            }

            if (notification.NotificationType == "Under7Days")
            {
                return $"Urgent {itemType} Expiry Alert";
            }

            return $"{itemType} Expiry Reminder";
        }

        private static string GenerateEmailBody(
            Notification notification)
        {
            return
                "Asset & Licence Tracker\n\n" +

                notification.Message + "\n\n" +

                "Please review the relevant client record " +
                "in the Asset & Licence Tracker.\n\n" +

                "This is an automated notification. " +
                "Please do not reply to this email.";
        }
    }
}
