namespace Ray.BiliBiliTool.Agent.BiliBiliAgent.Dtos
{
    public class BiliApiResponse : BiliApiResponse<object>
    {

    }

    public class BiliApiResponse<TData>
    {
        public int Code { get; set; } = int.MinValue;

        public string Message { get; set; }

        public TData Data { get; set; }
    }
}
