using Ray.BiliBiliTool.Agent;
using Ray.BiliBiliTool.Agent.BiliBiliAgent.Dtos.Mall;
using Ray.BiliBiliTool.Agent.BiliBiliAgent.Dtos.VipTask;

namespace Ray.BiliBiliTool.DomainService.Interfaces;

public interface IVipBigPointDomainService : IDomainService
{
    Task<VipBigPointCombine> GetCombineAsync(BiliCookie ck);

    Task VipExpressAsync(BiliCookie ck);

    Task SignAsync(BiliCookie ck);

    Task ReceiveDailyMissionsAsync(VipBigPointCombine combine, BiliCookie ck);

    Task ReceiveAndCompleteAsync(
        VipBigPointCombine info,
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
