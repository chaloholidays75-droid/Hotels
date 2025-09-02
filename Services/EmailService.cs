using MailKit.Net.Smtp;
using MailKit.Security;
using Microsoft.Extensions.Configuration;
using MimeKit;
using  EmailSettings = HotelAPI.Settings.EmailSettings;

namespace HotelAPI.Services
{
    public class EmailService 
    {
        private readonly IConfiguration _config;
        private readonly EmailSettings _emailSettings;

        public EmailService(IConfiguration config, EmailSettings emailSettings)
        {
            _emailSettings = emailSettings;
            _config = config;
        }

        public async Task SendEmailAsync(string toEmail, string subject, string body)
        {
            try
            {
                var message = new MimeMessage();
                message.From.Add(new MailboxAddress(_emailSettings.SenderName, _emailSettings.SenderEmail));
                message.To.Add(new MailboxAddress("", toEmail));
                message.Subject = subject;
                message.Body = new TextPart("plain") { Text = body };

                using var client = new SmtpClient();
                await client.ConnectAsync(_emailSettings.SmtpServer, _emailSettings.SmtpPort, MailKit.Security.SecureSocketOptions.StartTls);
                await client.AuthenticateAsync(_emailSettings.SenderEmail, _emailSettings.Password);
                await client.SendAsync(message);
                await client.DisconnectAsync(true);
            }
            catch (Exception ex)
            {
                // Log the error but do not throw
                Console.WriteLine($"⚠️ Failed to send email to {toEmail}: {ex.Message}");
            }
        }
    }
}