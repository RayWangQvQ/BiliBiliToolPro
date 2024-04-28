using Ray.BiliBiliTool.Agent.BiliBiliAgent.Dtos;
using System.Threading.Tasks;
using WebApiClientCore.Attributes;
using Ray.BiliBiliTool.Agent.BiliBiliAgent.Dtos.ViewMall;

namespace Ray.BiliBiliTool.Agent.BiliBiliAgent.Interfaces;

[Header("Host", "show.bilibili.com")]
public interface IVipMallApi
{
    [HttpPost("/api/activity/fire/common/event/dispatch")]
    Task<BiliApiResponse> ViewVipMallAsync([JsonContent] ViewVipMallRequest request);
}
