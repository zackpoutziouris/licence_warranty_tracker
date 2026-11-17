using System.ComponentModel.DataAnnotations.Schema;

namespace WebProject.Models
{
    [Table("ClientStaff")]
    public class ClientStaff
    {
        [Column("client_id")]
        public int ClientId { get; set; }

        [Column("staff_id")]
        public int StaffId { get; set; }

        // Navigation properties
        [ForeignKey(nameof(ClientId))]
        public Client Client { get; set; } = null!;

        [ForeignKey(nameof(StaffId))]
        public Staff Staff { get; set; } = null!;
    }
}