namespace Backend.Interfaces
{
    public interface IEmailService
    {
        // Full overload — explicit subject and HTML body
        void SendBookingEmail(string to, string subject, string htmlBody);

        // Convenience overload — sends a plain notification with a default subject
        void SendBookingEmail(string to, string subject);
    }
}
