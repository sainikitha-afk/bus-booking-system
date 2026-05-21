using System.ComponentModel.DataAnnotations.Schema;

namespace Backend.Models
{
    [Table("feedbacks")]
    public class Feedback
    {
        [Column("id")]
        public int Id { get; set; }

        [Column("user_id")]
        public int UserId { get; set; }

        [Column("bus_id")]
        public int BusId { get; set; }

        [Column("booking_group_id")]
        public int? BookingGroupId { get; set; }

        [Column("rating")]
        public int Rating { get; set; }   // 1 – 5

        [Column("message")]
        public string? Message { get; set; }

        [Column("submitted_at")]
        public DateTime SubmittedAt { get; set; } = DateTime.UtcNow;
    }
}
