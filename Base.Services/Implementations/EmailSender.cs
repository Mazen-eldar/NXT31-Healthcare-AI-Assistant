using Microsoft.AspNetCore.Identity.UI.Services;
using System.Net;
using System.Net.Mail;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace Base.Services.Implementations
{

    /// <summary>
    /// خدمة آمنة ووقائية لإرسال رسائل البريد الإلكتروني عبر SMTP.
    /// </summary>
    public class EmailSender : IEmailSender
    {
        private readonly IConfiguration _config;
        private readonly ILogger<EmailSender> _logger;

        // حقن IConfiguration و ILogger في البناء
        public EmailSender(IConfiguration config, ILogger<EmailSender> logger)
        {
            _config = config;
            _logger = logger;
        }

        public async Task SendEmailAsync(string email, string subject, string htmlMessage)
        {
            // 1. استخراج الإعدادات بأمان
            string? smtpHost = _config["Smtp:Host"];
            string? smtpPortStr = _config["Smtp:Port"];
            string? smtpUsername = _config["Smtp:Username"];
            string? smtpPassword = _config["Smtp:Password"];
            string? fromEmail = _config["Smtp:FromEmail"];
            string? fromName = "MyApp Support"; // اسم مرسل ثابت

            // التحقق من الإعدادات الأساسية
            if (string.IsNullOrEmpty(smtpHost) || string.IsNullOrEmpty(smtpPortStr) ||
                string.IsNullOrEmpty(smtpUsername) || string.IsNullOrEmpty(smtpPassword) ||
                string.IsNullOrEmpty(fromEmail))
            {
                _logger.LogError("SMTP configuration is missing or incomplete. Cannot send email to {Email}.", email);
                // إما أن نرمي استثناء أو نعود دون إرسال
                throw new InvalidOperationException("SMTP configuration is invalid.");
            }

            if (!int.TryParse(smtpPortStr, out int smtpPort))
            {
                _logger.LogError("SMTP port configuration is not a valid integer: {Port}.", smtpPortStr);
                throw new InvalidOperationException("SMTP port configuration is invalid.");
            }


            // 2. استخدام try-catch للتعامل مع أخطاء الاتصال والشبكة
            try
            {
                // استخدام using لضمان تحرير الموارد بشكل صحيح
                using var smtpClient = new SmtpClient(smtpHost)
                {
                    Port = smtpPort,
                    Credentials = new NetworkCredential(smtpUsername, smtpPassword),
                    EnableSsl = true,
                    DeliveryMethod = SmtpDeliveryMethod.Network,
                    Timeout = 10000 // تحديد مهلة زمنية (10 ثواني) لزيادة المرونة
                };

                using var mailMessage = new MailMessage
                {
                    From = new MailAddress(fromEmail, fromName),
                    Subject = subject,
                    Body = htmlMessage,
                    IsBodyHtml = true
                };

                mailMessage.To.Add(email);

                await smtpClient.SendMailAsync(mailMessage);

                _logger.LogInformation("Successfully sent email to {Email} with subject: {Subject}.", email, subject);
            }
            catch (SmtpException ex)
            {
                // خطأ خاص ببروتوكول SMTP (مثل فشل المصادقة، أو رفض الخادم)
                _logger.LogError(ex, "SMTP Error when sending email to {Email}. Status Code: {Status}.", email, ex.StatusCode);
                throw; // إعادة رمي الاستثناء للسماح بالمعالجة في طبقة أعلى إذا لزم الأمر
            }
            catch (Exception ex)
            {
                // أي خطأ آخر (مثل مشكلة في الاتصال بالشبكة)
                _logger.LogError(ex, "General error when sending email to {Email}.", email);
                throw;
            }
        }
    }
}
