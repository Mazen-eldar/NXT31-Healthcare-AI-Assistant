using Microsoft.AspNetCore.Http;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Base.Services.Interfaces
{
    public interface IUploadImageService
    {
        public  Task<string> UploadImageAsync(IFormFile file);
        public  Task<string> GetImageAsync(string ImageURL);
    }
}
