using System;
using System.Collections.Generic;

namespace Backend.Models
{
    public class BookingGroup
    {
        public int Id { get; set; }

        public int UserId { get; set; }
        public int BusId { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public string PaymentStatus { get; set; } = "Pending";

        public List<Booking> Bookings { get; set; } = new();
    }
}