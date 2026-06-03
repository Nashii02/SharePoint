using System.Net;
using System.Net.Mail;

namespace Sharepoint.Services
{
    public class EmailService
    {
        private readonly IConfiguration _config;

        public EmailService(IConfiguration config)
        {
            _config = config;
        }

        public async Task SendAsync(string toEmail, string subject, string body)
        {
            var s = _config.GetSection("EmailSettings");

            var client = new SmtpClient(s["SmtpHost"])
            {
                Port = int.Parse(s["SmtpPort"]!),
                Credentials = new NetworkCredential(s["SmtpUser"], s["SmtpPass"]),
                EnableSsl = true
            };

            var mail = new MailMessage
            {
                From = new MailAddress(s["FromEmail"]!, s["FromName"]),
                Subject = subject,
                Body = body,
                IsBodyHtml = true
            };

            mail.To.Add(toEmail);
            await client.SendMailAsync(mail);
        }

        public async Task SendOtpAsync(string toEmail, string otp)
        {
            await SendAsync(toEmail, "Your verification code", $@"
                <div style='font-family:sans-serif;max-width:480px;margin:auto;'>
                    <h2 style='color:#1B2A6B;'>Verify your account</h2>
                    <p>Use the code below to verify your email address.
                       It expires in <strong>10 minutes</strong>.</p>
                    <div style='font-size:36px;font-weight:bold;letter-spacing:10px;
                                background:#EEF2FF;color:#1B2A6B;padding:20px;
                                text-align:center;border-radius:8px;margin:24px 0;'>
                        {otp}
                    </div>
                    <p style='color:#9ca3af;font-size:12px;'>
                        If you did not request this, ignore this email.
                    </p>
                </div>");
        }
    }
}