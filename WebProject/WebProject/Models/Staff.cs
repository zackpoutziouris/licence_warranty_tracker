using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace WebProject.Models
{
    [Table("Staff")]
    public class Staff
    {
        [Key]
        [Column("staff_id")]
        public int StaffId { get; set; }

        [Required]
        [Column("staff_email")]
        public string StaffEmail { get; set; } = string.Empty;

        [Required]
        [Column("password_hash")]
        public string PasswordHash { get; set; } = string.Empty;

        [Column("is_admin")]
        public bool IsAdmin { get; set; } = false;
    }
}