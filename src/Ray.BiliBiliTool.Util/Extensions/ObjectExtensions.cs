using Newtonsoft.Json;

namespace System
{
    public static class ObjectExtensions
    {
        public static string ToJson<T>(this T obj, JsonSerializerSettings settings = null)
        {
            if (settings == null)
            {
                return JsonConvert.SerializeObject(obj);
            }
            else
            {
                return JsonConvert.SerializeObject(obj, settings);
            }
        }
    }
}
