using System;
using System.Diagnostics;
using System.Net.Http.Headers;
using System.Threading.Tasks;
using WebApiClientCore;
using WebApiClientCore.Attributes;

namespace Ray.BiliBiliTool.Agent.Attributes;

[DebuggerDisplay("{name} = {value}")]
[AttributeUsage(
    AttributeTargets.Method | AttributeTargets.Interface | AttributeTargets.Parameter,
    AllowMultiple = true
)]
public class AppendHeaderAttribute(
    string name,
    string value,
    AppendHeaderType appendHeaderType = AppendHeaderType.AddOrReplace
) : ApiActionAttribute, IApiParameterAttribute
{
    //添加的顺序为：子类、基类、子类函数、子类函数入参

    private readonly string _aliasName;

    public AppendHeaderAttribute(
        string aliasName,
        AppendHeaderType appendHeaderType = AppendHeaderType.AddOrReplace
    )
        : this(null, null, appendHeaderType)
    {
        _aliasName = aliasName;
    }

    public override Task OnRequestAsync(ApiRequestContext context)
    {
        AddByAppendType(context.HttpContext.RequestMessage.Headers, name, value);
        return Task.CompletedTask;
    }

    public Task OnRequestAsync(ApiParameterContext context)
    {
        string parameterName = _aliasName;
        if (string.IsNullOrEmpty(parameterName))
        {
            parameterName = context.ParameterName;
        }

        string text = context.ParameterValue?.ToString();
        if (!string.IsNullOrEmpty(text))
        {
            AddByAppendType(context.HttpContext.RequestMessage.Headers, parameterName, text);
        }

        return Task.CompletedTask;
    }

    private void AddByAppendType(HttpRequestHeaders headers, string key, string value)
    {
        switch (appendHeaderType)
        {
            case AppendHeaderType.Add:
                headers.TryAddWithoutValidation(key, value);
                break;
            case AppendHeaderType.AddIfNotExist:
                if (!headers.Contains(key))
                    headers.TryAddWithoutValidation(key, value);
                break;
            case AppendHeaderType.AddOrReplace:
                if (headers.Contains(key))
                    headers.Remove(key);
                headers.TryAddWithoutValidation(key, value);
                break;
            default:
                break;
        }
    }
}
