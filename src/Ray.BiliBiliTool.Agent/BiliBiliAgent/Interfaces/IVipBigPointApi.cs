using System;
using System.Collections.Generic;
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
    [Header("Referer", "https://big.bilibili.com/mobile/bigPoint/task")]
    [Header("User-Agent", "Mozilla/5.0 (Linux; Android 6.0.1; MuMu Build/V417IR; wv) AppleWebKit/537.36 (KHTML, like Gecko) Version/4.0 Chrome/66.0.3359.158 Mobile Safari/537.36 os/android model/MuMu build/6720300 osVer/6.0.1 sdkInt/23 network/2 BiliApp/6720300 mobi_app/android channel/html5_search_baidu Buvid/XZFC135F5263B6897C8A4BE7AEB125BBF10F8 sessionID/72d3f4c9 innerVer/6720310 c_locale/zh_CN s_locale/zh_CN disable_rcmd/0 6.72.0 os/android model/MuMu mobi_app/android build/6720300 channel/html5_search_baidu innerVer/6720310 osVer/6.0.1 network/2")]
    [LogFilter]
    public interface IVipBigPointApi
    {
        [HttpGet("/x/vip_point/task/combine")]
        Task<BiliApiResponse<VipTaskInfo>> GetTaskList();

        [HttpPost("/pgc/activity/score/task/sign")]
        Task<BiliApiResponse> Sign([FormContent] SignRequest request);

        [HttpPost("/pgc/activity/score/task/receive")]
        Task<BiliApiResponse> Receive([JsonContent] ReceiveOrCompleteTaskRequest request);

        [HttpPost("/pgc/activity/score/task/complete")]
        Task<BiliApiResponse> Complete([JsonContent] ReceiveOrCompleteTaskRequest request);

        [HttpPost("/pgc/activity/deliver/task/complete")]
        Task<BiliApiResponse> ViewComplete([FormContent] ViewRequest request);

        [HttpGet("/x/vip/privilege/my")]
        Task<BiliApiResponse<VouchersInfoResponse>> GetVouchersInfo();

        [Header("Content-Type", "application/x-www-form-urlencoded; charset=UTF-8")]
        [HttpPost("/x/vip/experience/add")]
        Task<BiliApiResponse> GetVipExperience([FormContent] VipExperienceRequest request);
    }
}
