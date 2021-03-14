using Ray.BiliBiliTool.Agent.Attributes;
using WebApiClientCore.Attributes;

namespace Ray.BiliBiliTool.Agent.BiliBiliAgent.Interfaces
{
    [Header("Accept", "application/json, text/plain, */*")]
    //[Header("Accept-Encoding", "gzip, deflate, br")]
    [Header("Accept-Encoding", "deflate, br")]
    [Header("Accept-Language", "zh-CN,zh;q=0.9,en;q=0.8,en-GB;q=0.7,en-US;q=0.6")]

    [Header("Sec-Fetch-Dest", "empty")]
    [Header("Sec-Fetch-Mode", "cors")]
    [Header("Sec-Fetch-Site", "same-site")]

    [Header("Connection", "keep-alive")]

    [LogFilter]
    public interface IBiliBiliApi
    {
    }
}
