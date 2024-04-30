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
    [Header("User-Agent", "Mozilla/5.0 (Linux; Android 12; SM-S9080 Build/V417IR; wv) AppleWebKit/537.36 (KHTML, like Gecko) Version/4.0 Chrome/91.0.4472.114 Mobile Safari/537.36 os/android model/SM-S9080 build/7760700 osVer/12 sdkInt/32 network/2 BiliApp/7760700 mobi_app/android channel/bili innerVer/7760710 c_locale/zh_CN s_locale/zh_CN disable_rcmd/0 7.76.0 os/android model/SM-S9080 mobi_app/android build/7760700 channel/bili innerVer/7760710 osVer/12 network/2")]
    [LogFilter]
    public interface IVipBigPointApi
    {
        /// <summary>
        /// 获取任务列表
        /// </summary>
        /// <returns></returns>
        [Header("Referer", "https://big.bilibili.com/mobile/bigPoint/task")]
        [HttpGet("/x/vip_point/task/combine")]
        Task<BiliApiResponse<VipTaskInfo>> GetTaskListAsync();

        /// <summary>
        /// 签到任务
        /// </summary>
        /// <param name="request"></param>
        /// <returns></returns>
        [Header("Referer", "https://big.bilibili.com/mobile/bigPoint/task")]
        [HttpPost("/pgc/activity/score/task/sign")]
        Task<BiliApiResponse> SignAsync([FormContent] SignRequest request);

        /// <summary>
        /// 领取任务
        /// </summary>
        /// <param name="request"></param>
        /// <returns></returns>
        [Header("Referer", "https://big.bilibili.com/mobile/bigPoint/task")]
        [HttpPost("/pgc/activity/score/task/receive")]
        Task<BiliApiResponse> Receive([JsonContent] ReceiveOrCompleteTaskRequest request);

        [Header("Referer", "https://big.bilibili.com/mobile/bigPoint/task")]
        [HttpPost("/pgc/activity/score/task/receive/v2")]
        Task<BiliApiResponse> ReceiveV2([FormContent] ReceiveOrCompleteTaskRequest request);


        [Header("Referer", "https://big.bilibili.com/mobile/bigPoint/task")]
        [HttpPost("/pgc/activity/score/task/complete")]
        Task<BiliApiResponse> CompleteAsync([JsonContent] ReceiveOrCompleteTaskRequest request);

        [Header("Referer", "https://big.bilibili.com/mobile/bigPoint/task")]
        [HttpPost("/pgc/activity/score/task/complete/v2")]
        Task<BiliApiResponse> CompleteV2([FormContent] ReceiveOrCompleteTaskRequest request);


        [Header("Referer", "https://big.bilibili.com/mobile/bigPoint/task")]
        [HttpPost("/pgc/activity/deliver/task/complete")]
        Task<BiliApiResponse> ViewComplete([FormContent] ViewRequest request);

        [Header("Referer", "https://big.bilibili.com/mobile/bigPoint/task")]
        [HttpGet("/x/vip/privilege/my")]
        Task<BiliApiResponse<VouchersInfoResponse>> GetVouchersInfoAsync();

        /// <summary>
        /// 兑换大会员经验
        /// </summary>
        /// <param name="request"></param>
        /// <returns></returns>
        [Header("Referer", "https://big.bilibili.com/mobile/bigPoint/task")]
        [HttpPost("/x/vip/experience/add")]
        Task<BiliApiResponse> ObtainVipExperienceAsync([FormContent] VipExperienceRequest request);
    }
}
