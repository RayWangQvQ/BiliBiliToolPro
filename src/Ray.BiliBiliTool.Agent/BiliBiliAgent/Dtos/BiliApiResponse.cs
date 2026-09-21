namespace Ray.BiliBiliTool.Agent.BiliBiliAgent.Dtos;

public class BiliApiResponse
{
    public int Code { get; set; } = int.MinValue;

    public string? Message { get; set; }
}

public class BiliApiResponse<TData> : BiliApiResponse
{
    /// <summary>
    /// 业务数据。B 站返回错误信封（如 {"code":-400,"message":"请求错误","ttl":1}）时该字段整体缺失，
    /// 因此必须可空：声明为 required 会让 System.Text.Json 在解析阶段就抛异常，
    /// 从而把真实的业务错误码掩盖成 Refit 的 "An error occured deserializing the response."。
    /// </summary>
    public TData? Data { get; set; }
}
