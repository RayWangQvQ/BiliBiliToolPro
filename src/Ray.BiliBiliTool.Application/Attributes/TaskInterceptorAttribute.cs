using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Text;
using MethodBoundaryAspect.Fody.Attributes;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Ray.BiliBiliTool.Infrastructure;

namespace Ray.BiliBiliTool.Application.Attributes
{
    /// <summary>
    /// 任务拦截器
    /// </summary>
    public class TaskInterceptorAttribute : OnMethodBoundaryAspect
    {
        private readonly ILogger _logger;
        private readonly string _taskName;
        private readonly TaskLevel _taskLevel;
        private readonly bool _rethrowWhenException;

        public TaskInterceptorAttribute(string taskName = null, TaskLevel taskLevel = TaskLevel.Two, bool rethrowWhenException = true)
        {
            _taskName = taskName;
            _taskLevel = taskLevel;
            _rethrowWhenException = rethrowWhenException;

            _logger = Global.ServiceProviderRoot.GetRequiredService<ILogger<TaskInterceptorAttribute>>();
        }

        public override void OnEntry(MethodExecutionArgs arg)
        {
            if (_taskName == null) return;
            string end = _taskLevel == TaskLevel.One ? Environment.NewLine : "";
            string delimiter = GetDelimiters();
            _logger.LogInformation(delimiter + "开始 {taskName} " + delimiter + end, _taskName);
        }

        public override void OnExit(MethodExecutionArgs arg)
        {
            if (_taskName == null) return;

            string delimiter = GetDelimiters();
            var append = new string(GetDelimiter(), _taskName.Length);

            _logger.LogInformation(delimiter + append + "结束" + append + delimiter + Environment.NewLine);
        }

        public override void OnException(MethodExecutionArgs arg)
        {
            if (_rethrowWhenException)
            {
                _logger.LogError("程序发生异常：{msg}", arg.Exception.Message);
                base.OnException(arg);
                return;
            }

            _logger.LogError("{task}失败，继续其他任务。失败信息:{msg}" + Environment.NewLine, _taskName, arg.Exception.Message);
            arg.FlowBehavior = FlowBehavior.Continue;
        }

        private string GetDelimiters()
        {
            char delimiter = GetDelimiter();

            int count = Convert.ToInt32(_taskLevel.DefaultValue());
            return new string(delimiter, count);
        }

        private char GetDelimiter()
        {
            switch (_taskLevel)
            {
                case TaskLevel.One:
                    return '=';
                case TaskLevel.Two:
                    return '-';
                case TaskLevel.Three:
                    return '-';
                default:
                    throw new ArgumentOutOfRangeException(nameof(_taskLevel), _taskLevel, null);
            }
        }
    }

    public enum TaskLevel
    {
        [DefaultValue(5)]
        One,

        [DefaultValue(3)]
        Two,

        [DefaultValue(2)]
        Three,
    }
}
