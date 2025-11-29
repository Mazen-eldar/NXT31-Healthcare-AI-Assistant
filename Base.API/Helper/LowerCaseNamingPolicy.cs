using System.Text.Json;

namespace Base.API.Helper
{
    public class LowerCaseNamingPolicy : JsonNamingPolicy
    {
        public override string ConvertName(string name)
        {
            // تحويل كل المفاتيح إلى lowercase
            return name.ToLower();
        }
    }
}
