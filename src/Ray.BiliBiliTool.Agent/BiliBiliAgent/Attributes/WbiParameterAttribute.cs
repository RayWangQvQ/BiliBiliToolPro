using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Ray.BiliBiliTool.Agent.BiliBiliAgent.Services;
using Ray.BiliBiliTool.Infrastructure.Cookie;
using WebApiClientCore;
using WebApiClientCore.Attributes;

namespace Ray.BiliBiliTool.Agent.BiliBiliAgent.Attributes;

[AttributeUsage(AttributeTargets.Parameter)]
public class WbiParameterAttribute : Attribute, IApiParameterAttribute
{
    public async Task OnRequestAsync(ApiParameterContext context)
    {
        // 只处理实现了IWrid接口的参数
        if (context.ParameterValue is IWrid wridRequest)
        {
            // 从依赖注入获取WbiService
            var wbiService = context.HttpContext.ServiceProvider.GetRequiredService<IWbiService>();

            // 从函数参数中获取Cookie
            string cookieStr = string.Empty;
            var allParameters = context.ActionDescriptor.Parameters;
            foreach (var parameter in allParameters)
            {
                var cookieHeader = parameter.Attributes.FirstOrDefault(a =>
                    a is HeaderAttribute header
                    && (string)header.GetFieldValue("aliasName") == "Cookie"
                );
                if (cookieHeader != null)
                {
                    cookieStr = context.Arguments[parameter.Index].ToString();
                    break;
                }
            }

            var cookie = CookieStrFactory<BiliCookie>.CreateNew(cookieStr);

            // 设置w_rid和wts值
            await wbiService.SetWridAsync(wridRequest, cookie);
        }
    }
}
