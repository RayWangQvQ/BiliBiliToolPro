using System.Threading;
using System.Threading.Tasks;
using Ray.BiliBiliTool.Application.Contracts;

namespace Ray.BiliBiliTool.Application
{
    public abstract class AppService : IAppService
    {
        public abstract Task DoTaskAsync(CancellationToken cancellationToken = default);
    }
}
