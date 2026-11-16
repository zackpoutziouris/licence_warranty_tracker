using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace WebProject.Models
{
    [Table("Audit")]
    public class Audit
    {
        [Key]
        [Column("audit_id")]
        public int AuditId { get; set; }

        [Required]
        [Column("audit_message")]
        public string AuditMessage { get; set; } = string.Empty;

        [Required]
        [Column("audit_date_time")]
        public string AuditDateTime { get; set; } = string.Empty;
    }
}
