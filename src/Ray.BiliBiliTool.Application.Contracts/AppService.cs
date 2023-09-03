using System.Threading;
using System.Threading.Tasks;

namespace Ray.BiliBiliTool.Application.Contracts
{
    public abstract class AppService : IAppService
    {
        public abstract Task DoTaskAsync(CancellationToken cancellationToken = default);
    }
}
