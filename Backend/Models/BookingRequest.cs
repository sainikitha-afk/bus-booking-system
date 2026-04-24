namespace Backend.Models
{
    public class BookingRequest
    {
        public int UserId { get; set; }
        public int BusId { get; set; }
        public int SeatNumber { get; set; }
        public string PassengerName { get; set; }
        public int Age { get; set; }
        public string Gender { get; set; }
    }

    public class FinalizeGroupRequest
    {
        public int UserId { get; set; }
        public int BusId { get; set; }
        public List<int> BookingIds { get; set; } = new();
    }
}