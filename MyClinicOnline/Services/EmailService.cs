using System.Net;
using System.Net.Mail;

namespace MyClinicOnline.Services
{
    public class EmailService : IEmailService
    {
        public async Task SendEmailAsync(string toEmail, string subject, string body)
        {
            try
            {
                var fromAddress = new MailAddress("maartin.simeonov@gmail.com", "MyClinicOnline");
                var toAddress = new MailAddress(toEmail);
                const string fromPassword = "rkgrophwmddyftza"; // Your App Password

                using var smtp = new SmtpClient
                {
                    Host = "smtp.gmail.com",
                    Port = 587,
                    EnableSsl = true,
                    DeliveryMethod = SmtpDeliveryMethod.Network,
                    UseDefaultCredentials = false,
                    Credentials = new NetworkCredential(fromAddress.Address, fromPassword)
                };

                using var message = new MailMessage(fromAddress, toAddress) { Subject = subject, Body = body };
                await smtp.SendMailAsync(message);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine("Email Error: " + ex.Message);
            }
        }
    }
}