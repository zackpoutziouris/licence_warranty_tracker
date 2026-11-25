
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace WebProject.Models
{
    [Table("Notifications")]
    public class Notification
    {
        [Key]
        [Column("notification_id")]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int NotificationId { get; set; }

        [Required]
        [Column("message")]
        public string Message { get; set; } = string.Empty;

        [Column("has_been_emailed")]
        public bool HasBeenEmailed { get; set; } = false;

        [Column("has_been_cleared")]
        public bool HasBeenCleared { get; set; } = false;

        [Column("assigned_staff")]
        public int AssignedStaff { get; set; }

        [Column("client_id")]
        public int ClientId { get; set; }

        [Required]
        [Column("item_type")]
        public string ItemType { get; set; } = string.Empty;

        [Column("item_id")]
        public int ItemId { get; set; }

        [Required]
        [Column("expiry_date")]
        public string ExpiryDate { get; set; } = string.Empty;

        [Required]
        [Column("notification_type")]
        public string NotificationType { get; set; } = string.Empty;

        [Required]
        [Column("notification_date")]
        public string NotificationDate { get; set; } = string.Empty;

        // Navigation property
        [ForeignKey(nameof(AssignedStaff))]
        public Staff Staff { get; set; } = null!;
    }
}
