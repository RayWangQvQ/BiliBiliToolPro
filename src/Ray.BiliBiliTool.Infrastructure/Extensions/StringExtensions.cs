using Newtonsoft.Json;

namespace System
{
    public static class StringExtensions
    {
        public static T ToObject<T>(this string str, JsonSerializerSettings settings = null)
        {
            if (settings == null)
            {
                return JsonConvert.DeserializeObject<T>(str);
            }
            else
            {
                return JsonConvert.DeserializeObject<T>(str, settings);
            }
        }
    }
}
