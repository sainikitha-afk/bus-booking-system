using System.ComponentModel.DataAnnotations.Schema;

namespace Backend.Models
{
    [Table("operator_profiles")]
    public class OperatorProfile
    {
        [Column("id")]
        public int Id { get; set; }

        [Column("user_id")]
        public int UserId { get; set; }

        [Column("business_name")]
        public string BusinessName { get; set; } = "";

        [Column("phone")]
        public string? Phone { get; set; }

        [Column("is_approved")]
        public bool IsApproved { get; set; } = false;

        [Column("rejection_reason")]
        public string? RejectionReason { get; set; }

        [Column("applied_at")]
        public DateTime AppliedAt { get; set; } = DateTime.UtcNow;
    }

    // Routes an operator has chosen to operate on
    [Table("operator_routes")]
    public class OperatorRoute
    {
        [Column("id")]
        public int Id { get; set; }

        [Column("operator_user_id")]
        public int OperatorUserId { get; set; }

        [Column("route_id")]
        public int RouteId { get; set; }

        // Operator's office address at this location → becomes pickup/drop point
        [Column("office_address")]
        public string? OfficeAddress { get; set; }
    }
}
