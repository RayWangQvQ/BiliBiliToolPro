namespace Ray.BiliBiliTool.Application.Contracts;

/// <summary>
/// 支持「只跑某一个账号」的任务服务，供补做功能使用。
/// </summary>
public interface IAccountTaskAppService : IAppService
{
    /// <summary>任务键：具体 AppService 的类型名</summary>
    string TaskKey { get; }

    /// <summary>只对该账号执行任务；账号不存在时抛异常</summary>
    Task DoTaskForAccountAsync(long userId, CancellationToken cancellationToken = default);
}
