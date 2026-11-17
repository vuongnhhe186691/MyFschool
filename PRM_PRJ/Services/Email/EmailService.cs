using System.Net;
using System.Net.Mail;
using PRM_PRJ.DTO.Config;

namespace PRM_PRJ.Services.Email
{
    public class EmailService : IEmailService
    {
        private readonly SmtpSettings _smtpSettings;

        public EmailService(IConfiguration configuration)
        {
            _smtpSettings = configuration.GetSection("Smtp").Get<SmtpSettings>() ?? new SmtpSettings();
        }

        public async Task SendOtpAsync(string toEmail, string otpCode)
        {
            await SendEmailAsync(
                toEmail,
                "Mã OTP đặt lại mật khẩu",
                $"Mã OTP của bạn là: {otpCode}. Mã có hiệu lực trong 5 phút.");
        }

        public async Task SendLoginOtpAsync(string toEmail, string otpCode)
        {
            await SendEmailAsync(
                toEmail,
                "Mã OTP đăng nhập",
                $"Mã OTP đăng nhập của bạn là: {otpCode}. Mã có hiệu lực trong 5 phút.");
        }

        private async Task SendEmailAsync(string toEmail, string subject, string body)
        {
            if (string.IsNullOrWhiteSpace(_smtpSettings.Host)
                || string.IsNullOrWhiteSpace(_smtpSettings.SenderEmail)
                || string.IsNullOrWhiteSpace(_smtpSettings.Username)
                || string.IsNullOrWhiteSpace(_smtpSettings.Password))
            {
                throw new InvalidOperationException("Cấu hình SMTP chưa đầy đủ");
            }

            using var client = new SmtpClient(_smtpSettings.Host, _smtpSettings.Port)
            {
                EnableSsl = _smtpSettings.EnableSsl,
                Credentials = new NetworkCredential(_smtpSettings.Username, _smtpSettings.Password)
            };

            using var message = new MailMessage
            {
                From = new MailAddress(_smtpSettings.SenderEmail, _smtpSettings.SenderName),
                Subject = subject,
                Body = body,
                IsBodyHtml = false
            };

            message.To.Add(toEmail);
            await client.SendMailAsync(message);
        }
    }
}