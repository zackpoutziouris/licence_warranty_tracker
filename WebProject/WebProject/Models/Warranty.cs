using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace WebProject.Models
{
    [Table("Warranties")]
    public class Warranty
    {
        [Key]
        [Column("warranty_id")]
        public int WarrantyId { get; set; }

        [Required]
        [Column("client_id")]
        public int ClientId { get; set; }

        [Required]
        [Column("asset_name")]
        public string AssetName { get; set; } = string.Empty;

        [Required]
        [Column("expiry_date")]
        public string ExpiryDate { get; set; } = string.Empty;

        // Navigation property
        [ForeignKey(nameof(ClientId))]
        public Client Client { get; set; } = null!;
    }
}