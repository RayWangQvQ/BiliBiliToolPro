using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;
using WebApiClientCore;
using WebApiClientCore.Attributes;

namespace Ray.BiliBiliTool.Agent.Attributes
{
    [DebuggerDisplay("{name} = {value}")]
    [AttributeUsage(AttributeTargets.Method | AttributeTargets.Interface | AttributeTargets.Parameter, AllowMultiple = true, Inherited = true)]
    public class AppendHeaderAttribute : ApiActionAttribute, IApiParameterAttribute, IApiAttribute
    {
        //添加的顺序为：子类、基类、子类函数、子类函数入参

        private readonly string _name;
        private readonly string _value;
        private readonly AppendHeaderType _appendHeaderType;
        private readonly string _aliasName;

        public AppendHeaderAttribute(string name, string value, AppendHeaderType appendHeaderType = AppendHeaderType.AddOrReplace)
        {
            _name = name;
            _value = value;
            _appendHeaderType = appendHeaderType;
        }

        public AppendHeaderAttribute(string aliasName, AppendHeaderType appendHeaderType = AppendHeaderType.AddOrReplace)
        {
            _aliasName = aliasName;
            _appendHeaderType = appendHeaderType;
        }

        public override Task OnRequestAsync(ApiRequestContext context)
        {
            AddByAppendType(context.HttpContext.RequestMessage.Headers, _name, _value);
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
            switch (_appendHeaderType)
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

    public enum AppendHeaderType
    {
        Add,
        AddIfNotExist,
        AddOrReplace
    }
}
