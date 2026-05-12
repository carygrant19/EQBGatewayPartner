using Microsoft.AspNetCore.Http;
using Newtonsoft.Json;
using System.Text;

namespace Gateway.Helper
{
    public static class SessionExtensions
    {
        public static void SetObject(this ISession session, string key, object value)
        {
            session.SetString(key, JsonConvert.SerializeObject(value));
        }

        public static T? GetObject<T>(this ISession session, string key)
        {
            var value = session.GetString(key);
            return value == null ? default : JsonConvert.DeserializeObject<T>(value);
        }

        public static bool HeadOffice(HttpContext httpContext)
        {
            var HO = httpContext!.Session.GetObject<List<string>>("HeadOffice")!;
            if (HO.Contains(httpContext!.Session.GetString("BranchCode")!))
                return true;
            else
                return false;
        }
    }
}
