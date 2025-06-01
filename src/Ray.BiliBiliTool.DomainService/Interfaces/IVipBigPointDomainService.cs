using Ray.BiliBiliTool.Agent;
using Ray.BiliBiliTool.Agent.BiliBiliAgent.Dtos.VipTask;

namespace Ray.BiliBiliTool.DomainService.Interfaces;

public interface IVipBigPointDomainService : IDomainService
{
    Task<VipTaskInfo> GetTaskListAsync(BiliCookie ck);

    Task VipExpressAsync(BiliCookie ck);

    Task Sign(BiliCookie ck);

    Task ReceiveTasksAsync(VipTaskInfo info, BiliCookie ck);

    Task ReceiveAndCompleteAsync(
        VipTaskInfo info,
        string moduleCode,
        string taskCode,
        BiliCookie ck,
        Func<string, BiliCookie, Task<bool>> completeFunc
    );

    Task<bool> CompleteAsync(string taskCode, BiliCookie ck);

    Task<bool> CompleteViewAsync(string taskCode, BiliCookie ck);

    Task<bool> CompleteViewVipMallAsync(string taskCode, BiliCookie ck);

    Task<bool> CompleteV2Async(string taskCode, BiliCookie ck);
}
