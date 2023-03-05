using Microsoft.Extensions.Logging;
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
        private static Dictionary<int, string> _index = new Dictionary<int, string>();

        static TaskTypeFactory()
        {
            Assembly assembly = Assembly.GetAssembly(typeof(TaskTypeFactory));
            var types = assembly.GetTypes()
                .Where(x => x.IsAssignableTo(typeof(IAppService))
                            && x.IsInterface);
            var index = 1;
            foreach (var type in types)
            {
                string code = type.GetCustomAttribute<DescriptionAttribute>()?.Description;
                if (!string.IsNullOrWhiteSpace(code))
                {
                    _dic.Add(code, type);
                    _index.Add(index, code);
                    index++;
                }
            }
        }

        public static Type Create(string code)
        {
            return _dic[code];
        }

        public static void Show(ILogger logger)
        {
            foreach (var item in _index)
            {
                logger.LogInformation("{index}):{code}", item.Key, item.Value);
            }
        }

        public static string GetCodeByIndex(int index)
        {
            return _index[index];
        }
    }
}
