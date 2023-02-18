using System.Threading;
using System.Threading.Tasks;

namespace Ray.BiliBiliTool.Application.Contracts
{
    /// <summary>
    /// 定义一个AppService
    /// </summary>
    public interface IAppService
    {
        Task DoTaskAsync(CancellationToken cancellationToken);
    }
}
