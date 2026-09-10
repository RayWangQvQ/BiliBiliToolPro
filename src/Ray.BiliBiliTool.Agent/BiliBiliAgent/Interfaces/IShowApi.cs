using Ray.BiliBiliTool.Agent.BiliBiliAgent.Dtos;
using Ray.BiliBiliTool.Agent.BiliBiliAgent.Dtos.ShowApi;
using Refit;

namespace Ray.BiliBiliTool.Agent.BiliBiliAgent.Interfaces;

[Headers("Host: show.bilibili.com")]
public interface IShowApi
{
    [Post("/api/activity/fire/common/event/dispatch")]
    Task<BiliApiResponse> ViewVipMallAsync(
        [Body] ViewVipMallRequest request,
        [Header("Cookie")] string ck
    );
}
