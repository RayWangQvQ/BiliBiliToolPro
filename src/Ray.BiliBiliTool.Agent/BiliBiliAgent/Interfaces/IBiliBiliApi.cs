using Refit;

namespace Ray.BiliBiliTool.Agent.BiliBiliAgent.Interfaces
{
    [Headers(
    "Accept:application/json, text/plain, */*",
    "Referer:https://www.bilibili.com/",
    "Connection:keep-alive"
    )]
    public interface IBiliBiliApi
    {
    }
}
