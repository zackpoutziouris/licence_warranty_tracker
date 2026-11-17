using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace WebProject.Models
{
    [Table("Clients")]
    public class Client
    {
        [Key]
        [Column("client_id")]
        public int ClientId { get; set; }

        [Required]
        [Column("client_name")]
        public string ClientName { get; set; } = string.Empty;

        // Navigation properties
        public ICollection<Warranty> Warranties { get; set; }
            = new List<Warranty>();

        public ICollection<License> Licenses { get; set; }
            = new List<License>();

        public ICollection<ClientStaff> ClientStaff { get; set; }
            = new List<ClientStaff>();
    }
}