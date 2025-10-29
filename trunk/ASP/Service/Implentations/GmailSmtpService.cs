using ASP.Services;  // Để implement EmailServiceInterface
using MailKit.Net.Smtp;
using MailKit.Security;
using MimeKit;

namespace ASP.Service.Implentations
{
    public class GmailSmtpService //: EmailServiceInterface
    {
        private readonly string _smtpHost = "smtp.gmail.com";
        private readonly int _smtpPort = 587;
        private readonly string _senderEmail = "minhduongqh0311@gmail.com";  // ← Sender của bạn
        private readonly string _appPassword = "rujv uzyy rpjn wnun";  // ← DÁN App Password 16 ký tự ở đây

        public async Task SendEmailSMTP(string to, string cc, string subject, string body, bool isHtml = true)
        {
            try
            {
                Console.WriteLine($"🔍 Gmail SMTP: Preparing email to={to}, cc={cc}, subject={subject}");  // Log cho test

                var message = new MimeMessage();
                message.From.Add(MailboxAddress.Parse(_senderEmail));  // Sender cố định
                message.To.Add(MailboxAddress.Parse(to));  // To: anhqh0311@gmail.com (sẽ override ở Controller)
                if (!string.IsNullOrEmpty(cc))
                    message.Cc.Add(MailboxAddress.Parse(cc));  // CC: minhduongqh0311@gmail.com
                message.Subject = subject;

                var bodyBuilder = new BodyBuilder();
                if (isHtml)
                    bodyBuilder.HtmlBody = body;  // HTML body
                else
                    bodyBuilder.TextBody = body;  // Plain text fallback
                message.Body = bodyBuilder.ToMessageBody();

                using var client = new SmtpClient();
                await client.ConnectAsync(_smtpHost, _smtpPort, SecureSocketOptions.StartTls);
                await client.AuthenticateAsync(_senderEmail, _appPassword);  // Auth với App Password
                await client.SendAsync(message);
                await client.DisconnectAsync(true);

                Console.WriteLine($"✅ Gmail SMTP SENT SUCCESS: From={_senderEmail} to={to}, cc={cc}");  // Log success
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Gmail SMTP FAILED: {ex.Message}\nStackTrace: {ex.StackTrace}");
                throw;  // Throw để Controller catch và log
            }
        }
    }
}