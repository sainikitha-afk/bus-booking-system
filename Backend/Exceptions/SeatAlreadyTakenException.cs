namespace Backend.Exceptions
{
    // Thrown when a seat is already booked, paid, or actively locked by another user.
    public class SeatAlreadyTakenException : BusBookingException
    {
        public int BusId     { get; }
        public int SeatNumber { get; }

        public SeatAlreadyTakenException(int busId, int seatNumber)
            : base($"Seat {seatNumber} on bus {busId} is already taken or locked.", statusCode: 409)
        {
            BusId      = busId;
            SeatNumber = seatNumber;
        }
    }
}