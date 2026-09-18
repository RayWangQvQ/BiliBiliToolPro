using Ray.BiliBiliTool.Agent.BiliBiliAgent.Interfaces;

namespace Ray.BiliBiliTool.Agent.BiliBiliAgent.Dtos.ApiApi.Relation;

public class GetFollowingsRequest
{
    public GetFollowingsRequest(
        long userId,
        FollowingsOrderType followingsOrder = FollowingsOrderType.AttentionDesc
    )
    {
        vmid = userId;
        order_type = followingsOrder.DefaultValue();
    }

    public long vmid { get; set; }

    public string order_type { get; set; }

    public int pn { get; set; } = 1;

    public int ps { get; set; } = 20;

    public string order { get; set; } = "desc";

    public string jsonp { get; set; } = "jsonp";

    //public string callback { get; set; }
}
