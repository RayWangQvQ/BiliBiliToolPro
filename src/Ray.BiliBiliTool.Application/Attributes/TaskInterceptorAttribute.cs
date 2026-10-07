using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Ray.BiliBiliTool.Application.Contracts;
using Ray.BiliBiliTool.Application.Diagnostics;
using Ray.BiliBiliTool.Domain;
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
        TaskRecoveryProgressScope.Report(
            $"stage/{context.Method.Name}",
            taskName,
            TaskRecoveryProgressState.Running,
            "正在执行"
        );
        string end = taskLevel == TaskLevel.One ? Environment.NewLine : "";
        string delimiter = GetDelimiters();
        _logger.LogInformation(delimiter + "开始 {taskName} " + delimiter + end, taskName);
    }

    public override void OnExit(MethodContext context)
    {
        if (taskName == null)
            return;
        TaskRecoveryProgressScope.Report(
            $"stage/{context.Method.Name}",
            taskName,
            TaskRecoveryProgressState.Completed,
            "动作执行结束"
        );

        string delimiter = GetDelimiters();
        var append = new string(GetDelimiter(), taskName.Length);

        _logger.LogInformation(
            delimiter + append + "结束" + append + delimiter + Environment.NewLine
        );
    }

    public override void OnException(MethodContext context)
    {
        if (context.Exception is { } exception)
            TaskRecoveryProgressScope.Report(
                $"stage/{context.Method.Name}",
                taskName ?? "任务动作",
                TaskRecoveryProgressState.Failed,
                TaskRecoveryProgressScope.DescribeFailure(exception)
            );
        if (context.Exception is not OperationCanceledException)
            TaskExecutionFailureScope.MarkFailed();
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
        TaskFlowDiagnosticScope.RecordHandledFailure(context.Exception);
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
