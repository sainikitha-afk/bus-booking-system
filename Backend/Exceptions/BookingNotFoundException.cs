namespace Backend.Exceptions
{
    // Thrown when a booking or booking group ID does not exist in the database.
    public class BookingNotFoundException : BusBookingException
    {
        public BookingNotFoundException(int id, string entity = "Booking")
            : base($"{entity} with ID {id} was not found.", statusCode: 404)
        {
        }
    }
}