namespace Backend.Exceptions
{
    // Thrown when the SMTP email send fails after all retries.
    // Wraps the original MailKit/network exception as InnerException.
    public class EmailDeliveryException : BusBookingException
    {
        public string RecipientEmail { get; }

        public EmailDeliveryException(string recipientEmail, Exception innerException)
            : base($"Failed to deliver email to {recipientEmail}.", innerException, statusCode: 502)
        {
            RecipientEmail = recipientEmail;
        }
    }
}
