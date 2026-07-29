namespace Ray.BiliBiliTool.Agent.BiliBiliAgent.Dtos;

public class BiliApiResponse
{
    public int Code { get; set; } = int.MinValue;

    public string? Message { get; set; }
}

public class BiliApiResponse<TData> : BiliApiResponse
{
    public TData Data { get; set; } = default!;
}
