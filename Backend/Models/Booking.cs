using System.ComponentModel.DataAnnotations.Schema;

namespace Backend.Models
{
    [Table("bookings")]
    public class Booking
    {
        [Column("id")]
        public int Id { get; set; }

        [Column("user_id")]
        public int UserId { get; set; }

        [Column("bus_id")]
        public int BusId { get; set; }

        [Column("seat_number")]
        public int SeatNumber { get; set; }

        [Column("booking_time")]
        public DateTime BookingTime { get; set; } = DateTime.UtcNow;

        [Column("status")]
        public string Status { get; set; } = "Booked";

        [Column("lock_expiry")]
        public DateTime? LockExpiry { get; set; }

        [Column("payment_status")]
        public string PaymentStatus { get; set; } = "Pending";

        [Column("passenger_name")]
        public string? PassengerName { get; set; }

        [Column("age")]
        public int? Age { get; set; }

        [Column("gender")]
        public string? Gender { get; set; }

        [Column("booking_group_id")]
        public int? BookingGroupId { get; set; }
        
        [ForeignKey("BookingGroupId")]
        public BookingGroup? BookingGroup { get; set; }
    }
}
