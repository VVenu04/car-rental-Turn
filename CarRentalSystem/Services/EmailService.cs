using System.Net;
using System.Net.Mail;

namespace CarRentalSystem.Services
{
    public class EmailService : IEmailService
    {
        private readonly IConfiguration _configuration;

        // Inject IConfiguration to read settings from secrets.json
        public EmailService(IConfiguration configuration)
        {
            _configuration = configuration;
        }

        public async Task SendEmailAsync(string toEmail, string subject, string body)
        {
            // Get the settings from secrets.json
            var emailSettings = _configuration.GetSection("EmailSettings");
            var senderEmail = emailSettings["SenderEmail"];
            var senderName = emailSettings["SenderName"];
            var appPassword = emailSettings["AppPassword"];
            var smtpServer = emailSettings["SmtpServer"];
            var port = int.Parse(emailSettings["Port"]);

            // Create the email message
            var mailMessage = new MailMessage
            {
                From = new MailAddress(senderEmail, senderName),
                Subject = subject,
                Body = body,
                IsBodyHtml = true,
            };
            mailMessage.To.Add(toEmail);

            // Configure the SMTP client and send the email
            using (var client = new SmtpClient(smtpServer, port))
            {
                client.Credentials = new NetworkCredential(senderEmail, appPassword);
                client.EnableSsl = true; // Gmail requires SSL

                await client.SendMailAsync(mailMessage);
            }
        }
    }
}