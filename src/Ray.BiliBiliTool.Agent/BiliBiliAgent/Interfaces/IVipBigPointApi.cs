using System.Threading.Tasks;
using Ray.BiliBiliTool.Agent.Attributes;
using Ray.BiliBiliTool.Agent.BiliBiliAgent.Dtos;
using Ray.BiliBiliTool.Agent.BiliBiliAgent.Dtos.VipTask;
using WebApiClientCore.Attributes;

namespace Ray.BiliBiliTool.Agent.BiliBiliAgent.Interfaces
{
    /// <summary>
    /// 大会员大积分
    /// </summary>
    [Header("Host", "api.bilibili.com")]
    [Header("User-Agent",
        "Mozilla/5.0 (Linux; Android 9; SM-N9700 Build/PQ3A.190605.04081832; wv) AppleWebKit/537.36 (KHTML, like Gecko) Version/4.0 Chrome/91.0.4472.114 Safari/537.36 Mobile os/android model/SM-N9700 build/7300400 osVer/9 sdkInt/28 network/2 BiliApp/7300400 mobi_app/android channel/alifenfa Buvid/XY77D6C72ECDC63147110C5C8D1DA34D38CD1 sessionID/9795ed5c innerVer/7300400 c_locale/zh_CN s_locale/zh_CN disable_rcmd/0 7.30.0 os/android model/SM-N9700 mobi_app/android build/7300400 channel/alifenfa innerVer/7300400 osVer/9 network/2")]
    [LogFilter]
    public interface IVipBigPointApi
    {
        [Header("Referer", "https://big.bilibili.com/mobile/bigPoint/task")]
        [HttpGet("/x/vip_point/task/combine")]
        Task<BiliApiResponse<VipTaskInfo>> GetTaskList();


        [Header("Referer", "https://www.bilibili.com")]
        [HttpPost("/pgc/activity/score/task/sign")]
        Task<BiliApiResponse> Sign([FormContent] SignRequest request);

        [Header("Referer", "https://big.bilibili.com/mobile/bigPoint/task")]
        [HttpPost("/pgc/activity/score/task/receive")]
        Task<BiliApiResponse> Receive([JsonContent] ReceiveOrCompleteTaskRequest request);

        [Header("Referer", "https://big.bilibili.com/mobile/bigPoint/task")]
        [HttpPost("/pgc/activity/score/task/receive/v2")]
        Task<BiliApiResponse> ReceiveV2([FormContent] ReceiveOrCompleteTaskRequest request);


        [Header("Referer", "https://big.bilibili.com/mobile/bigPoint/task")]
        [HttpPost("/pgc/activity/score/task/complete")]
        Task<BiliApiResponse> Complete([JsonContent] ReceiveOrCompleteTaskRequest request);

        [Header("Referer", "https://big.bilibili.com/mobile/bigPoint/task")]
        [HttpPost("/pgc/activity/score/task/complete/v2")]
        Task<BiliApiResponse> CompleteV2([FormContent] ReceiveOrCompleteTaskRequest request);


        [Header("Referer", "https://big.bilibili.com/mobile/bigPoint/task")]
        [HttpPost("/pgc/activity/deliver/task/complete")]
        Task<BiliApiResponse> ViewComplete([FormContent] ViewRequest request);

        [Header("Referer", "https://big.bilibili.com/mobile/bigPoint/task")]
        [HttpGet("/x/vip/privilege/my")]
        Task<BiliApiResponse<VouchersInfoResponse>> GetVouchersInfo();

        [Header("Referer", "https://big.bilibili.com/mobile/bigPoint/task")]
        // [Header("Content-Type", "application/x-www-form-urlencoded; charset=UTF-8")]
        [HttpPost("/x/vip/experience/add")]
        Task<BiliApiResponse> GetVipExperience([FormContent] VipExperienceRequest request);
    }
}
