namespace Ray.BiliBiliTool.Application.Contracts;

public interface IAppService
{
    Task DoTaskAsync(CancellationToken cancellationToken = default);
}
