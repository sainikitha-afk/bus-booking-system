namespace Backend.Exceptions
{
    // Thrown when the 5-minute seat lock window has passed before the user completed booking.
    public class SeatLockExpiredException : BusBookingException
    {
        public int SeatNumber { get; }

        public SeatLockExpiredException(int seatNumber)
            : base($"The lock on seat {seatNumber} has expired. Please re-select the seat.", statusCode: 410)
        {
            SeatNumber = seatNumber;
        }
    }
}