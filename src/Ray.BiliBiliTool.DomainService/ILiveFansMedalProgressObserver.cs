using Ray.BiliBiliTool.Agent;
using Ray.BiliBiliTool.Agent.BiliBiliAgent.Dtos.LiveApi;

namespace Ray.BiliBiliTool.DomainService;

public interface ILiveFansMedalProgressObserver
{
    void Report(
        BiliCookie cookie,
        long anchorId,
        ActivatedMedalResponse progress,
        DateTimeOffset observedAt
    );
}
