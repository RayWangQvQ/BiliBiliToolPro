using System.Reflection;
using System.Text.RegularExpressions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using WebApiClientCore;
using WebApiClientCore.Attributes;

namespace Ray.BiliBiliTool.Agent.Attributes;

public class LogFilterAttribute(bool logError = true) : LoggingFilterAttribute
{
    /// <summary>
    /// 需要脱敏的 Cookie 键名（登录凭证 / CSRF Token 等）。
    /// 值会被替换为 ***，避免敏感凭据写入日志文件。
    /// </summary>
    private static readonly string[] SensitiveCookieKeys =
    {
        "SESSDATA",
        "bili_jct",
        "DedeUserID",
        "DedeUserID__ckMd5",
        "sid",
        "buvid3",
        "buvid4",
        "b_nut",
    };

    protected override Task WriteLogAsync(ApiResponseContext context, LogMessage logMessage)
    {
        var loggerFactory = context.HttpContext.ServiceProvider.GetService<ILoggerFactory>();
        if (loggerFactory == null)
        {
            return Task.CompletedTask;
        }

        MethodInfo member = context.ActionDescriptor.Member;
        var strArray = new string?[5];
        var declaringType1 = member.DeclaringType;
        strArray[0] = declaringType1?.Namespace;
        strArray[1] = ".";
        var declaringType2 = member.DeclaringType;
        strArray[2] = declaringType2?.Name;
        strArray[3] = ".";
        strArray[4] = member.Name;
        string categoryName = string.Concat(strArray);
        ILogger logger = loggerFactory.CreateLogger(categoryName);

        // 对原始日志内容做敏感信息脱敏后再输出
        string safeMessage = RedactSensitiveData(logMessage.ToString());

        if (logMessage.Exception == null)
        {
            logger.LogDebug("{message}", safeMessage);
        }
        else
        {
            if (logError)
                logger.LogError("{message}", safeMessage);
            else
                logger.LogDebug("{message}", safeMessage);
        }

        return Task.CompletedTask;
    }

    /// <summary>
    /// 对日志中的敏感信息（Cookie / Set-Cookie 头）进行脱敏。
    /// 覆盖两种形式：
    ///   1. Cookie 请求头：Cookie: SESSDATA=xxx; bili_jct=yyy
    ///   2. Set-Cookie 响应头：Set-Cookie: SESSDATA=xxx; Path=/; ...
    /// 避免登录凭据泄露到日志文件。
    /// </summary>
    private static string RedactSensitiveData(string message)
    {
        if (string.IsNullOrEmpty(message))
        {
            return message;
        }

        foreach (var key in SensitiveCookieKeys)
        {
            // 匹配 "Key=任意非(;或,或空白)字符" 的形式，保留 Key，隐藏值
            message = Regex.Replace(
                message,
                $@"({key}\s*=\s*)[^;,\s]+",
                "$1***",
                RegexOptions.IgnoreCase
            );
        }

        return message;
    }
}
