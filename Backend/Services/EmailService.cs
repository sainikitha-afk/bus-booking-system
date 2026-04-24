using MailKit.Net.Smtp;
using MimeKit;

namespace Backend.Services
{
    public class EmailService
    {
        private readonly string _fromEmail;
        private readonly string _appPassword;

        public EmailService(IConfiguration config)
        {
            _fromEmail   = config["EmailSettings:FromEmail"]   ?? "";
            _appPassword = config["EmailSettings:AppPassword"] ?? "";
        }

        public void SendBookingEmail(string toEmail, string subject, string htmlBody)
        {
            if (string.IsNullOrEmpty(_fromEmail) || _fromEmail.StartsWith("YOUR_"))
            {
                Console.WriteLine("[Email] Not configured — skipping send.");
                return;
            }

            var message = new MimeMessage();
            message.From.Add(MailboxAddress.Parse(_fromEmail));
            message.To.Add(MailboxAddress.Parse(toEmail));
            message.Subject = subject;
            message.Body = new TextPart("html") { Text = htmlBody };

            using var smtp = new SmtpClient();
            smtp.Connect("smtp.gmail.com", 587, false);
            smtp.Authenticate(_fromEmail, _appPassword);
            smtp.Send(message);
            smtp.Disconnect(true);
        }
    }
}
