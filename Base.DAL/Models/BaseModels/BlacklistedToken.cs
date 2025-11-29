using Base.DAL.Models.BaseModels;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Base.Shared.Responses
{
    public class BlacklistedToken:BaseEntity
    {
        public string Token { get; set; }
        public DateTime ExpiryDate { get; set; }
    }
}
