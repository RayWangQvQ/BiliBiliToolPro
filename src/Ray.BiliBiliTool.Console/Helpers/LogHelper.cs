using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Ray.BiliBiliTool.Infrastructure;

namespace Ray.BiliBiliTool.Console.Helpers
{
    public class LogHelper
    {
        /// <summary>
        /// 获取配置的Console的日志等级
        /// </summary>
        /// <returns></returns>
        public static Serilog.Events.LogEventLevel GetConsoleLogLevel()
        {
            var consoleLevelStr = RayConfiguration.Root["Serilog:WriteTo:0:Args:restrictedToMinimumLevel"];
            if (string.IsNullOrWhiteSpace(consoleLevelStr)) consoleLevelStr = "Information";

            Serilog.Events.LogEventLevel levelEnum = (Serilog.Events.LogEventLevel)
                Enum.Parse(typeof(Serilog.Events.LogEventLevel), consoleLevelStr);

            return levelEnum;
        }

        /// <summary>
        /// 获取配置的Console的日志模板
        /// </summary>
        /// <returns></returns>
        public static string GetConsoleLogTemplate()
        {
            var templateStr = RayConfiguration.Root["Serilog:WriteTo:0:Args:outputTemplate"];
            if (string.IsNullOrWhiteSpace(templateStr)) templateStr = "[{Timestamp:HH:mm:ss} {Level:u3}] {Message:lj}{NewLine}{Exception}";

            return templateStr;
        }
    }
}
