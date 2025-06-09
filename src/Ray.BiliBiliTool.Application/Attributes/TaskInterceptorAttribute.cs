using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Ray.BiliBiliTool.Infrastructure;
using Rougamo;
using Rougamo.Context;

namespace Ray.BiliBiliTool.Application.Attributes;

/// <summary>
/// 任务拦截器
/// </summary>
public class TaskInterceptorAttribute(
    string? taskName = null,
    TaskLevel taskLevel = TaskLevel.Two,
    bool rethrowWhenException = true
) : MoAttribute
{
    private readonly ILogger _logger = Global.ServiceProviderRoot!.GetRequiredService<
        ILogger<TaskInterceptorAttribute>
    >();

    public override void OnEntry(MethodContext context)
    {
        if (taskName == null)
            return;
        string end = taskLevel == TaskLevel.One ? Environment.NewLine : "";
        string delimiter = GetDelimiters();
        _logger.LogInformation(delimiter + "开始 {taskName} " + delimiter + end, taskName);
    }

    public override void OnExit(MethodContext context)
    {
        if (taskName == null)
            return;

        string delimiter = GetDelimiters();
        var append = new string(GetDelimiter(), taskName.Length);

        _logger.LogInformation(
            delimiter + append + "结束" + append + delimiter + Environment.NewLine
        );
    }

    public override void OnException(MethodContext context)
    {
        if (rethrowWhenException)
        {
            _logger.LogError("程序发生异常：{msg}", context.Exception?.Message ?? "");
            base.OnException(context);
            return;
        }

        _logger.LogError(
            "{task}失败，继续其他任务。失败信息:{msg}" + Environment.NewLine,
            taskName,
            context.Exception?.Message ?? ""
        );
        context.HandledException(this, null);
    }

    private string GetDelimiters()
    {
        char delimiter = GetDelimiter();

        int count = Convert.ToInt32(taskLevel.DefaultValue());
        return new string(delimiter, count);
    }

    private char GetDelimiter()
    {
        return taskLevel switch
        {
            TaskLevel.One => '=',
            TaskLevel.Two => '-',
            TaskLevel.Three => '-',
            _ => throw new ArgumentOutOfRangeException(nameof(taskLevel), taskLevel, null),
        };
    }
}
