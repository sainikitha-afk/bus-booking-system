using System.ComponentModel.DataAnnotations.Schema;

namespace Backend.Models
{
    [Table("platform_fees")]
    public class PlatformFee
    {
        [Column("id")]
        public int Id { get; set; }

        // "Fixed" or "Percentage"
        [Column("fee_type")]
        public string FeeType { get; set; } = "Fixed";

        [Column("amount")]
        public decimal Amount { get; set; } = 50;

        [Column("is_active")]
        public bool IsActive { get; set; } = true;
    }
}
