using System;
using System.Collections.Generic;
using System.Text;
using MethodBoundaryAspect.Fody.Attributes;
using Ray.BiliBiliTool.Agent.ServerChanAgent;
using Serilog;

namespace Ray.BiliBiliTool.Application.Attributes
{
    /// <summary>
    /// 任务拦截器
    /// </summary>
    public class TaskInterceptorAttribute : OnMethodBoundaryAspect
    {
        private readonly ILogger _logger;
        private readonly string _taskName;
        private readonly bool _rethrowWhenException;

        public TaskInterceptorAttribute(string taskName = null, bool rethrowWhenException = true)
        {
            _taskName = taskName;
            _rethrowWhenException = rethrowWhenException;

            _logger = Log.Logger;//todo:暂时没想到从容器注入ILogger的方法,这里直接用了Serilog的静态Log
        }

        public override void OnEntry(MethodExecutionArgs arg)
        {
            if (_taskName == null) return;

            PushService.PushStringWriter.WriteLine("#### >\r\n");
            _logger.Information("---开始【{taskName}】---", _taskName);
        }

        public override void OnExit(MethodExecutionArgs arg)
        {
            if (_taskName == null) return;

            _logger.Information("---结束---\r\n", _taskName);
        }

        public override void OnException(MethodExecutionArgs arg)
        {
            if (_rethrowWhenException)
            {
                _logger.Fatal("程序发生异常：{msg}", arg.Exception.Message);
                base.OnException(arg);
                return;
            }

            _logger.Fatal("{task}失败，继续其他任务。Msg:{msg}\r\n", _taskName, arg.Exception.Message);
            arg.FlowBehavior = FlowBehavior.Continue;
        }
    }
}
