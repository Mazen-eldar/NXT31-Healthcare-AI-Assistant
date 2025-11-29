using Base.Services.Interfaces;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static Base.Services.Implementations.UploadImageService;

namespace Base.Services.Implementations
{
    public class UploadImageService : IUploadImageService
    {
        private readonly IWebHostEnvironment _env; 
        private readonly IHttpContextAccessor _httpContextAccessor;

        public UploadImageService( IWebHostEnvironment env, IHttpContextAccessor httpContextAccessor)
        {
            _env = env;
            _httpContextAccessor = httpContextAccessor;
        }

        public async Task<string> UploadImageAsync(IFormFile file)
        {
            if (file == null || file.Length == 0)
                throw new Exception("File is empty");

            // نوع الملف
            var allowedExt = new[] { ".jpg", ".jpeg", ".png" };
            var ext = Path.GetExtension(file.FileName).ToLower();

            if (!allowedExt.Contains(ext))
                throw new Exception("Invalid file type");
            var nameWithoutExt = Path.GetFileNameWithoutExtension(file.FileName);

            // اسم الصورة الجديد
            var fileName = $"{nameWithoutExt}_{Guid.NewGuid()}{ext}";
            var webRoot = _env.WebRootPath ?? Path.Combine(Directory.GetCurrentDirectory(), "wwwroot");

            // مكان الحفظ
            var folderPath = Path.Combine(webRoot, "uploads");
            if (!Directory.Exists(folderPath)) Directory.CreateDirectory(folderPath);

            var filePath = Path.Combine(folderPath, fileName);

            using (var stream = new FileStream(filePath, FileMode.Create))
            {
                await file.CopyToAsync(stream);
            }

            // حفظ الرابط في الداتا بيز
            var ImagePath = $"uploads/{fileName}";

            return ImagePath;
        }
        public async Task<string> GetImageAsync(string path) {
            var request = _httpContextAccessor.HttpContext?.Request;

            var baseUrl = $"{request.Scheme}://{request.Host}";
            return $"{baseUrl}/{path.Replace("\\", "/")}";
        }
    }

}
