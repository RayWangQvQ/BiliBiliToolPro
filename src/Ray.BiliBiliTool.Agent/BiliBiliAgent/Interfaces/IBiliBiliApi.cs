using Ray.BiliBiliTool.Agent.Attributes;
using WebApiClientCore.Attributes;

namespace Ray.BiliBiliTool.Agent.BiliBiliAgent.Interfaces
{
    [AddIfNotExistHeader("Accept", "application/json, text/plain, */*")]
    //[AddIfNotExistHeader("Accept-Encoding", "gzip, deflate, br")]
    [AddIfNotExistHeader("Accept-Language", "zh-CN,zh;q=0.9,en;q=0.8,en-GB;q=0.7,en-US;q=0.6")]

    [AddIfNotExistHeader("Sec-Fetch-Dest", "empty")]
    [AddIfNotExistHeader("Sec-Fetch-Mode", "cors")]
    [AddIfNotExistHeader("Sec-Fetch-Site", "same-site")]

    [AddIfNotExistHeader("Connection", "keep-alive")]

    [LogFilter]
    public interface IBiliBiliApi
    {
    }
}
