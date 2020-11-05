using System.Threading.Tasks;
using Ray.BiliBiliTool.Agent.BiliBiliAgent.Dtos;
using Refit;

namespace Ray.BiliBiliTool.Agent.BiliBiliAgent.Interfaces
{
    public interface IExperienceApi
    {
        /// <summary>
        /// 获取通过投币已获取的经验值
        /// </summary>
        /// <returns></returns>
        [Get("/plus/account/exp.php")]
        [Headers(
            "accept: text/html,application/xhtml+xml,application/xml;q=0.9,image/webp,image/apng,*/*;q=0.8,application/signed-exchange;v=b3;q=0.9",
            //"accept-encoding:gzip, deflate, br",
            "accept-language:zh-CN,zh;q=0.9,en;q=0.8,en-GB;q=0.7,en-US;q=0.6")]
        Task<ExperienceByDonateCoin> GetDonateCoinExp();
    }
}
