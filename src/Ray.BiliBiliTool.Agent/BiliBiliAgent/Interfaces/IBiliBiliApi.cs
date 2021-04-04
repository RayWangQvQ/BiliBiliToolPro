using Ray.BiliBiliTool.Agent.Attributes;
using WebApiClientCore.Attributes;

namespace Ray.BiliBiliTool.Agent.BiliBiliAgent.Interfaces
{
    [AppendHeader("Accept", "application/json, text/plain, */*", AppendHeaderType.AddIfNotExist)]
    //[Header("Accept-Encoding", "gzip, deflate, br")]
    [AppendHeader("Accept-Language", "zh-CN,zh;q=0.9,en;q=0.8,en-GB;q=0.7,en-US;q=0.6", AppendHeaderType.AddIfNotExist)]

    [AppendHeader("Sec-Fetch-Dest", "empty", AppendHeaderType.AddIfNotExist)]
    [AppendHeader("Sec-Fetch-Mode", "cors", AppendHeaderType.AddIfNotExist)]
    [AppendHeader("Sec-Fetch-Site", "same-site", AppendHeaderType.AddIfNotExist)]

    [AppendHeader("Connection", "keep-alive", AppendHeaderType.AddIfNotExist)]

    [LogFilter]
    public interface IBiliBiliApi
    {
    }
}
