using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text;
using MethodDecorator.Fody.Interfaces;
//using Microsoft.Extensions.Logging;
using Serilog;

namespace Ray.BiliBiliTool.DomainService.Attributes
{
    public class LogIntercepterAttribute : Attribute, IMethodDecorator
    {
        private readonly string _taskName;
        private readonly ILogger _logger;
        private object _instance;
        private MethodBase _method;
        private object[] _args;

        public LogIntercepterAttribute(string taskName)
        {
            _taskName = taskName;
            _logger = Log.Logger;//todo:暂时没想到注入ILogger的方法,这里直接用来Serilog的静态Log
        }

        public void Init(object instance, MethodBase method, object[] args)
        {
            _instance = instance;
            _method = method;
            _args = args;
        }

        public void OnEntry()
        {
            _logger.Information("-----开始【{taskName}】-----", _taskName);
        }

        public void OnException(Exception exception)
        {
        }

        public void OnExit()
        {
            _logger.Information("-----【{taskName}】结束-----\r\n", _taskName);
        }
    }
}
