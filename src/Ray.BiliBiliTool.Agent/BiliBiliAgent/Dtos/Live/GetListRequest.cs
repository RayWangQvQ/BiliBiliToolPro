using Ray.BiliBiliTool.Agent.BiliBiliAgent.Services;

namespace Ray.BiliBiliTool.Agent.BiliBiliAgent.Dtos.Live;

public class GetListRequest : IWrid
{
    public string platform { get; set; } = "web";
    public long parent_area_id { get; set; }
    public long area_id { get; set; }
    public string sort_type { get; set; }
    public int page { get; set; }
    public long wts { get; set; }
    public string w_rid { get; set; }
}
