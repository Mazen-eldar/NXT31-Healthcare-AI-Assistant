using Base.Services.Interfaces;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Microsoft.AspNetCore.Identity.UI.Services;

namespace Base.Services.Implementations
{
    public class EmailService : IEmailService
    {
        private readonly IConfiguration _config;
        private readonly ILogger<EmailService> _logger;
        private readonly IEmailSender _emailSender;

        public EmailService(IConfiguration config, ILogger<EmailService> logger, IEmailSender emailSender)
        {
            _config = config;
            _logger = logger;
            _emailSender = emailSender;
        }

        public async Task SendOtpEmailAsync(string to, string otp)
        {
            await _emailSender.SendEmailAsync(to, "Your OTP Code",
                    $"<p>Your OTP verification code is: <b>{otp}</b></p><p>It will expire in 5 minutes.</p>");
            // يمكن استخدام SMTP أو أي خدمة مثل SendGrid
            _logger.LogInformation($"Sending OTP {otp} to {to}");
            await Task.CompletedTask;
        }
    }
}
