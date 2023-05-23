using Newtonsoft.Json.Linq;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Reflection;

namespace Ray.BiliBiliTool.Infrastructure.Helpers
{
    public class ObjectHelper
    {
        public static Dictionary<string, object> ObjectToDictionary(object obj)
        {
            Dictionary<string, object> dict = new Dictionary<string, object>();

            // 获取对象的所有属性
            PropertyInfo[] properties = obj.GetType().GetProperties();

            // 遍历所有属性并将其添加到字典中
            foreach (PropertyInfo property in properties)
            {
                dict.Add(property.Name, property.GetValue(obj));
            }

            return dict;
        }
    }
}
