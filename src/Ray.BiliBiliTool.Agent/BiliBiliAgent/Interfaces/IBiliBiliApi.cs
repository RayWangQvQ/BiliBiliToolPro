using Ray.BiliBiliTool.Agent.Attributes;
using WebApiClientCore.Attributes;

namespace Ray.BiliBiliTool.Agent.BiliBiliAgent.Interfaces
{
    [Header("Accept", "application/json, text/plain, */*")]
    [Header("Referer", "https://www.bilibili.com/")]
    [Header("Connection", "keep-alive")]
    [LogFilter]
    public interface IBiliBiliApi
    {
    }
}
