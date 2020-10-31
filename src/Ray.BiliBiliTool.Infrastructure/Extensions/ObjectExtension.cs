using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Reflection;
using System.Text;

namespace Ray.BiliBiliTool.Infrastructure.Extensions
{
    public static class ObjectExtension
    {
        public static string GetPropertyDescription(this Type type, string propertyName)
        {
            DescriptionAttribute desc = (DescriptionAttribute)type?.GetProperty(propertyName)?
                .GetCustomAttributes(typeof(DescriptionAttribute), false)
                .FirstOrDefault();

            return desc?.Description;
        }
    }
}
