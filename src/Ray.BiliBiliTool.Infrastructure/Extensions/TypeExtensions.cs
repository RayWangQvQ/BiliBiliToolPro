using System.ComponentModel;
using System.Linq;

namespace System
{
    public static class TypeExtensions
    {
        /// <summary>
        /// 获取属性的Description
        /// </summary>
        /// <param name="type"></param>
        /// <param name="propertyName"></param>
        /// <returns></returns>
        public static string GetPropertyDescription(this Type type, string propertyName)
        {
            DescriptionAttribute desc = (DescriptionAttribute)type?.GetProperty(propertyName)?
                .GetCustomAttributes(typeof(DescriptionAttribute), false)
                .FirstOrDefault();

            return desc?.Description;
        }
    }
}
