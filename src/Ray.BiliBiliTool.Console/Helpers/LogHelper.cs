using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Ray.BiliBiliTool.Infrastructure;
using Serilog.Events;

namespace Ray.BiliBiliTool.Console.Helpers
{
    public class LogHelper
    {
        /// <summary>
        /// 获取配置的Console的日志等级
        /// </summary>
        /// <returns></returns>
        public static LogEventLevel GetConsoleLogLevel(IConfiguration configuration)
        {
            var consoleLevelStr = configuration["Serilog:WriteTo:0:Args:restrictedToMinimumLevel"];
            if (string.IsNullOrWhiteSpace(consoleLevelStr)) consoleLevelStr = "Information";

            LogEventLevel levelEnum = (LogEventLevel)
                Enum.Parse(typeof(LogEventLevel), consoleLevelStr);

            return levelEnum;
        }

        /// <summary>
        /// 获取配置的Console的日志模板
        /// </summary>
        /// <returns></returns>
        public static string GetConsoleLogTemplate(IConfiguration configuration)
        {
            var templateStr = configuration["Serilog:WriteTo:0:Args:outputTemplate"];
            if (string.IsNullOrWhiteSpace(templateStr)) templateStr = "[{Timestamp:HH:mm:ss} {Level:u3}] {Message:lj}{NewLine}{Exception}";

            return templateStr;
        }
    }
}
