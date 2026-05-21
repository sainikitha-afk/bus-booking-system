using MailKit.Net.Smtp;
using MimeKit;
using Backend.Interfaces;
using Backend.Exceptions;

namespace Backend.Services
{
    public class EmailService : IEmailService
    {
        private readonly string _fromEmail;
        private readonly string _appPassword;
        private readonly ILogger<EmailService> _logger;

        public EmailService(IConfiguration config, ILogger<EmailService> logger)
        {
            _fromEmail   = config["EmailSettings:FromEmail"]   ?? "";
            _appPassword = config["EmailSettings:AppPassword"] ?? "";
            _logger      = logger;
        }

        // Full overload — caller supplies subject and complete HTML body.
        // Demonstrates: try / catch (specific type) / finally for guaranteed SMTP disconnect.
        public void SendBookingEmail(string toEmail, string subject, string htmlBody)
        {
            if (string.IsNullOrEmpty(_fromEmail) || _fromEmail.StartsWith("YOUR_"))
            {
                _logger.LogWarning("[Email] SMTP not configured — skipping send to {Email}", toEmail);
                return;
            }

            var smtp = new SmtpClient();  // declared outside try so finally can reach it

            try
            {
                var message = new MimeMessage();
                message.From.Add(MailboxAddress.Parse(_fromEmail));
                message.To.Add(MailboxAddress.Parse(toEmail));
                message.Subject = subject;
                message.Body    = new TextPart("html") { Text = htmlBody };

                smtp.Connect("smtp.gmail.com", 587, false);
                smtp.Authenticate(_fromEmail, _appPassword);
                smtp.Send(message);

                _logger.LogInformation("[Email] Sent '{Subject}' to {Email}", subject, toEmail);
            }
            catch (FormatException ex)
            {
                // MailboxAddress.Parse throws this for a malformed email address
                _logger.LogError(ex, "[Email] Invalid recipient address: {Email}", toEmail);
                throw new EmailDeliveryException(toEmail, ex);
            }
            catch (MailKit.Security.AuthenticationException ex)
            {
                // Wrong app-password or Google account issue
                _logger.LogError(ex, "[Email] SMTP authentication failed for sender {Sender}", _fromEmail);
                throw new EmailDeliveryException(toEmail, ex);
            }
            catch (Exception ex)
            {
                // Network timeout, port blocked, any other SMTP failure
                _logger.LogError(ex, "[Email] SMTP send failed to {Email}", toEmail);
                throw new EmailDeliveryException(toEmail, ex);
            }
            finally
            {
                // finally guarantees the SMTP connection is always closed,
                // even if an exception was thrown mid-send.
                if (smtp.IsConnected)
                    smtp.Disconnect(true);

                smtp.Dispose();
                _logger.LogDebug("[Email] SMTP connection closed.");
            }
        }

        // Convenience overload — body auto-generated from subject line.
        public void SendBookingEmail(string toEmail, string subject)
        {
            var defaultBody = $@"
<div style='font-family:sans-serif;max-width:560px;margin:auto;padding:20px'>
  <h2 style='color:#e53935'>BusBooking Notification</h2>
  <p>{subject}</p>
  <p style='color:#888;font-size:12px;margin-top:20px'>Thank you for using BusBooking!</p>
</div>";
            SendBookingEmail(toEmail, subject, defaultBody);
        }
    }
}
