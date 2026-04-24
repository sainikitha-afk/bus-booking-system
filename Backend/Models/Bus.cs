using System.ComponentModel.DataAnnotations.Schema;

namespace Backend.Models
{
    [Table("buses")]
    public class Bus
    {
        [Column("id")]
        public int Id { get; set; }

        [Column("name")]
        public string Name { get; set; } = "";

        [Column("vehicle_reg_number")]
        public string? VehicleRegNumber { get; set; }

        [Column("seats_available")]
        public int SeatsAvailable { get; set; }

        [Column("price")]
        public int Price { get; set; }

        [Column("source")]
        public string? Source { get; set; }

        [Column("destination")]
        public string? Destination { get; set; }

        [Column("travel_date")]
        public DateTime? TravelDate { get; set; }

        [Column("departure_time")]
        public string? DepartureTime { get; set; }

        [Column("arrival_time")]
        public string? ArrivalTime { get; set; }

        [Column("bus_type")]
        public string? BusType { get; set; } = "AC";

        [Column("operator_name")]
        public string? OperatorName { get; set; }

        [Column("operator_user_id")]
        public int? OperatorUserId { get; set; }

        [Column("route_id")]
        public int? RouteId { get; set; }

        [Column("total_seats")]
        public int TotalSeats { get; set; } = 40;

        [Column("boarding_point")]
        public string? BoardingPoint { get; set; }

        [Column("drop_point")]
        public string? DropPoint { get; set; }

        [Column("is_women_only")]
        public bool IsWomenOnly { get; set; } = false;

        [Column("is_active")]
        public bool IsActive { get; set; } = true;

        // "2x2", "2x3", "sleeper"
        [Column("layout_type")]
        public string? LayoutType { get; set; } = "2x2";
    }
}
