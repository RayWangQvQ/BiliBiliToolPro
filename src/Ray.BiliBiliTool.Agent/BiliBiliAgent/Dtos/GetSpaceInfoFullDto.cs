using Ray.BiliBiliTool.Agent.BiliBiliAgent.Services;

namespace Ray.BiliBiliTool.Agent.BiliBiliAgent.Dtos;

public class GetSpaceInfoDto : IWrid
{
    public long mid { get; set; }

    public string w_rid { get; set; }
    public long wts { get; set; }
}
