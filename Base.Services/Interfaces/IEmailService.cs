using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Identity.UI.Services;

namespace Base.Services.Interfaces
{
    public interface IEmailService
    {
        Task SendOtpEmailAsync(string to, string otp);
    }
}
