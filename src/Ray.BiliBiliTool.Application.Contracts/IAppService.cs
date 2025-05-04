using System.Threading;
using System.Threading.Tasks;

namespace Ray.BiliBiliTool.Application.Contracts;

public interface IAppService
{
    Task DoTaskAsync(CancellationToken cancellationToken = default);
}
