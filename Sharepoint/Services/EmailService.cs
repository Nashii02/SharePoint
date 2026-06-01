using MailKit.Net.Smtp;
using MailKit.Security;
using MimeKit;

namespace Sharepoint.Services
{
    public class EmailService
    {
        private readonly IConfiguration _config;

        public EmailService(IConfiguration config)
        {
            _config = config;
        }

        public async Task SendPasswordResetAsync(string toEmail, string resetLink)
        {
            var host = _config["EmailSettings:Host"] ?? throw new InvalidOperationException("EmailSettings:Host is not configured.");
            var port = int.Parse(_config["EmailSettings:Port"] ?? "587");
            var username = _config["EmailSettings:Username"] ?? throw new InvalidOperationException("EmailSettings:Username is not configured.");
            var password = _config["EmailSettings:Password"] ?? throw new InvalidOperationException("EmailSettings:Password is not configured.");
            var fromName = _config["EmailSettings:FromName"] ?? "IT Solutions";

            var message = new MimeMessage();
            message.From.Add(new MailboxAddress(fromName, username));
            message.To.Add(MailboxAddress.Parse(toEmail));
            message.Subject = "Reset Your Password";

            message.Body = new TextPart("html")
            {
                Text = $@"
                <!DOCTYPE html>
                <html>
                <head>
                    <meta charset='utf-8'>
                    <style>
                        body {{ font-family: Arial, sans-serif; background: #ECFFEC; margin: 0; padding: 40px 20px; }}
                        .container {{ max-width: 480px; margin: 0 auto; background: white; border-radius: 16px; overflow: hidden; box-shadow: 0 4px 24px rgba(28,54,103,0.10); border: 1.5px solid #c8e6d4; }}
                        .header {{ background: #1C3667; padding: 32px; text-align: center; }}
                        .header h1 {{ color: white; font-size: 22px; margin: 0 0 4px 0; font-weight: 700; }}
                        .header p {{ color: rgba(255,255,255,0.55); font-size: 13px; margin: 0; }}
                        .body {{ padding: 32px; }}
                        .body p {{ font-size: 14px; color: #3a5a6a; line-height: 1.65; margin: 0 0 20px 0; }}
                        .btn {{ display: block; width: fit-content; margin: 0 auto 24px; padding: 14px 32px; background: #1C3667; color: white; text-decoration: none; border-radius: 10px; font-weight: 700; font-size: 15px; }}
                        .note {{ font-size: 12px; color: #a0b8a8; text-align: center; margin: 0; }}
                        .footer {{ background: #f7fff7; border-top: 1.5px solid #c8e6d4; padding: 16px 32px; text-align: center; font-size: 12px; color: #a0b8a8; }}
                    </style>
                </head>
                <body>
                    <div class='container'>
                        <div class='header'>
                            <h1>{fromName}</h1>
                            <p>Password Reset Request</p>
                        </div>
                        <div class='body'>
                            <p>We received a request to reset the password for your account. Click the button below to set a new password:</p>
                            <a href='{resetLink}' class='btn'>Reset My Password</a>
                            <p class='note'>This link will expire in <strong>1 hour</strong>. If you did not request a password reset, you can safely ignore this email.</p>
                        </div>
                        <div class='footer'>
                            &copy; {DateTime.Now.Year} {fromName}. All rights reserved.
                        </div>
                    </div>
                </body>
                </html>"
            };

            using var smtp = new SmtpClient();
            await smtp.ConnectAsync(host, port, SecureSocketOptions.StartTls);
            await smtp.AuthenticateAsync(username, password);
            await smtp.SendAsync(message);
            await smtp.DisconnectAsync(true);
        }
    }
}