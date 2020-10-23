using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using Refit;

namespace BiliBiliTool.Agent.Interfaces
{
    /// <summary>
    /// 漫画接口
    /// </summary>
    public interface IMangaApi
    {
        /// <summary>
        /// 漫画签到
        /// </summary>
        /// <param name="platform"></param>
        /// <returns></returns>
        [Post("/twirp/activity.v1.Activity/ClockIn?platform={platform}")]
        Task<BiliApiResponse> ClockIn(string platform);
    }
}
