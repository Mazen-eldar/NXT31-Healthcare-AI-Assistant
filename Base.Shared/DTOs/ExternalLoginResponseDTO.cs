using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Base.Shared.DTOs
{
    public class ExternalLoginResponseDTO
    {
        public string Token { get; set; }
        public object user { get; set; }
    }
}
