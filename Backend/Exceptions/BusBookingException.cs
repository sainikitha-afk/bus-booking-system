namespace Backend.Exceptions
{
    // Base class for all domain-specific exceptions in this application.
    // Inheriting from Exception gives us the full structured exception chain.
    public class BusBookingException : Exception
    {
        public int StatusCode { get; }

        public BusBookingException(string message, int statusCode = 500)
            : base(message)
        {
            StatusCode = statusCode;
        }

        public BusBookingException(string message, Exception innerException, int statusCode = 500)
            : base(message, innerException)
        {
            StatusCode = statusCode;
        }
    }
}