using Microsoft.EntityFrameworkCore;
using System.Globalization;
using WebProject.Data;
using WebProject.Helpers;
using WebProject.Models;

namespace WebProject.Services
{
    public class NotificationService
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<NotificationService> _logger;

        public NotificationService(
            ApplicationDbContext context,
            ILogger<NotificationService> logger)
        {
            _context = context;
            _logger = logger;
        }

        public async Task<int> GenerateNotificationsAsync()
        {
            DateTime today = UkDateHelper.Today();

            var clients = await _context.Clients
                .AsNoTracking()
                .Include(c => c.Warranties)
                .Include(c => c.Licenses)
                .Include(c => c.ClientStaff)
                .AsSplitQuery()
                .ToListAsync();

            int notificationsCreated = 0;

            foreach (var client in clients)
            {
                foreach (var assignment in client.ClientStaff)
                {
                    int staffId = assignment.StaffId;

                    // Check all warranties for this client
                    foreach (var warranty in client.Warranties)
                    {
                        notificationsCreated +=
                            await CreateNotificationAsync(
                                staffId,
                                client.ClientId,
                                client.ClientName,
                                "Warranty",
                                warranty.WarrantyId,
                                warranty.AssetName,
                                warranty.ExpiryDate,
                                today);
                    }

                    // Check all licences for this client
                    foreach (var license in client.Licenses)
                    {
                        notificationsCreated +=
                            await CreateNotificationAsync(
                                staffId,
                                client.ClientId,
                                client.ClientName,
                                "License",
                                license.LicenseId,
                                license.LicenseName,
                                license.ExpiryDate,
                                today);
                    }
                }
            }

            _logger.LogInformation(
                "Notification check completed. {Count} new notifications created.",
                notificationsCreated);

            return notificationsCreated;
        }

        private async Task<int> CreateNotificationAsync(
            int staffId,
            int clientId,
            string clientName,
            string itemType,
            int itemId,
            string itemName,
            string expiryDate,
            DateTime today)
        {
            // Convert the stored expiry date
            if (!DateTime.TryParseExact(
                expiryDate,
                "yyyy-MM-dd",
                CultureInfo.InvariantCulture,
                DateTimeStyles.None,
                out DateTime parsedExpiryDate))
            {
                _logger.LogWarning(
                    "Invalid expiry date for {ItemType} ID {ItemId}.",
                    itemType,
                    itemId);

                return 0;
            }

            int daysRemaining =
                (parsedExpiryDate.Date - today.Date).Days;

            string? notificationType =
                GetNotificationType(daysRemaining);

            // No notification required
            if (notificationType == null)
            {
                return 0;
            }

            string message = GenerateMessage(
                staffId,
                clientId,
                clientName,
                itemType,
                itemId,
                itemName,
                parsedExpiryDate,
                daysRemaining);

            string notificationDate = today.ToString(
                "yyyy-MM-dd",
                CultureInfo.InvariantCulture);

            // Insert the notification unless it already exists.
            // The unique index prevents duplicate records.
            int rowsInserted =
                await _context.Database.ExecuteSqlInterpolatedAsync($@"
                    INSERT INTO Notifications (
                        message,
                        has_been_emailed,
                        has_been_cleared,
                        assigned_staff,
                        client_id,
                        item_type,
                        item_id,
                        expiry_date,
                        notification_type,
                        notification_date
                    )
                    VALUES (
                        {message},
                        0,
                        0,
                        {staffId},
                        {clientId},
                        {itemType},
                        {itemId},
                        {expiryDate},
                        {notificationType},
                        {notificationDate}
                    )
                    ON CONFLICT (
                        assigned_staff,
                        item_type,
                        item_id,
                        expiry_date,
                        notification_type
                    )
                    DO NOTHING;
                ");

            return rowsInserted;
        }

        private static string? GetNotificationType(
            int daysRemaining)
        {
            if (daysRemaining < 0)
            {
                return $"Overdue_{Math.Abs(daysRemaining)}";
            }

            if (daysRemaining < 7)
            {
                return "Under7Days";
            }

            if (daysRemaining < 30)
            {
                return "Under30Days";
            }

            return null;
        }

        private static string GenerateMessage(
            int staffId,
            int clientId,
            string clientName,
            string itemType,
            int itemId,
            string itemName,
            DateTime expiryDate,
            int daysRemaining)
        {
            string displayItemType =
                itemType == "License" ? "Licence" : "Warranty";

            string formattedExpiryDate = expiryDate.ToString(
                "dd/MM/yyyy",
                CultureInfo.InvariantCulture);

            string messagePrefix =
                $"Staff ID {staffId} - " +
                $"Client ID {clientId} ({clientName}) - " +
                $"{displayItemType} ID {itemId} ({itemName}): ";

            if (daysRemaining < 0)
            {
                int daysOverdue = Math.Abs(daysRemaining);

                string dayText =
                    daysOverdue == 1 ? "day" : "days";

                return messagePrefix +
                    $"Expired on {formattedExpiryDate} " +
                    $"and is now {daysOverdue} {dayText} overdue.";
            }

            if (daysRemaining == 0)
            {
                return messagePrefix +
                    $"Expires today ({formattedExpiryDate}).";
            }

            string remainingDayText =
                daysRemaining == 1 ? "day" : "days";

            return messagePrefix +
                $"Expires on {formattedExpiryDate}. " +
                $"{daysRemaining} {remainingDayText} remaining.";
        }
    }
}
