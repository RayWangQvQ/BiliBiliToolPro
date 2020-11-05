using Refit;

namespace Ray.BiliBiliTool.Agent.BiliBiliAgent.Interfaces
{
    [Headers(
    "Accept:application/json, text/plain, */*",
    "Referer:https://www.bilibili.com/",
    "Connection:keep-alive",
    "User-Agent:Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/85.0.4183.121 Safari/537.36 Edg/85.0.564.70"
    )]
    public interface IBiliBiliApi
    {
    }
}
