using System;
using System.Reflection;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using WebApiClientCore;
using WebApiClientCore.Attributes;

namespace Ray.BiliBiliTool.Agent.Attributes
{
    public class LogFilterAttribute : LoggingFilterAttribute
    {
        private readonly bool _logError;

        public LogFilterAttribute(bool logError = true)
        {
            _logError = logError;
        }

        protected override Task WriteLogAsync(ApiResponseContext context, LogMessage logMessage)
        {
            ILoggerFactory service = context.HttpContext.ServiceProvider.GetService<ILoggerFactory>();
            if (service == null)
                return Task.CompletedTask;

            MethodInfo member = context.ApiAction.Member;
            string[] strArray = new string[5];
            Type declaringType1 = member.DeclaringType;
            strArray[0] = (object)declaringType1 != null ? declaringType1.Namespace : (string)null;
            strArray[1] = ".";
            Type declaringType2 = member.DeclaringType;
            strArray[2] = (object)declaringType2 != null ? declaringType2.Name : (string)null;
            strArray[3] = ".";
            strArray[4] = member.Name;
            string categoryName = string.Concat(strArray);
            ILogger logger = service.CreateLogger(categoryName);

            if (logMessage.Exception == null)
                logger.LogDebug(logMessage.ToString());//修改为Debug等级
            else
            {
                if (_logError)
                    logger.LogError(logMessage.ToString());
                else
                    logger.LogDebug(logMessage.ToString());
            }

            return Task.CompletedTask;
        }
    }
}
