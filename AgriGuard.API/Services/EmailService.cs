using System.Net;
using System.Net.Mail;

namespace AgriGuard.API.Services
{
    public class EmailService
    {
        private readonly IConfiguration _configuration;

        public EmailService(IConfiguration configuration)
        {
            _configuration = configuration;
        }

        public async Task SendEmailAsync(string toEmail, string subject, string body)
        {
            var fromEmail = _configuration["EmailSettings:From"];
            var username = _configuration["EmailSettings:Username"];
            var password = _configuration["EmailSettings:Password"];
            var smtpServer = _configuration["EmailSettings:SmtpServer"];
            var port = int.Parse(_configuration["EmailSettings:Port"]!);

            var message = new MailMessage();
            message.From = new MailAddress(fromEmail!, "AgriGuard Support");
            message.To.Add(toEmail);
            message.Subject = subject;
            message.Body = body;
            message.IsBodyHtml = true;

            using var client = new SmtpClient(smtpServer, port);
            client.Credentials = new NetworkCredential(username, password);
            client.EnableSsl = true;

            await client.SendMailAsync(message);
        }
    }
}