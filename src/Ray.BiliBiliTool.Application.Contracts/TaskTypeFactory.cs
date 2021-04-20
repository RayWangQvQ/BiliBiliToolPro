using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace Ray.BiliBiliTool.Application.Contracts
{
    public class TaskTypeFactory
    {
        private static Dictionary<string, Type> _dic = new Dictionary<string, Type>();

        static TaskTypeFactory()
        {
            Assembly assembly = Assembly.GetExecutingAssembly();
            var types = assembly.GetTypes()
                .Where(x => x.IsAssignableTo(typeof(IAppService))
                            && x.IsInterface);
            foreach (var type in types)
            {
                string code = type.GetCustomAttribute<DescriptionAttribute>()?.Description;
                if (!string.IsNullOrWhiteSpace(code))
                    _dic.Add(code, type);
            }
        }

        public static Type Create(string code)
        {
            return _dic[code];
        }
    }
}
