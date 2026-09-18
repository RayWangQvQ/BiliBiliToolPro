namespace Ray.BiliBiliTool.Agent.BiliBiliAgent.Dtos.ApiApi.Relation;

public class GetSpecialFollowingsRequest
{
    public GetSpecialFollowingsRequest(long userId)
    {
        mid = userId;
    }

    public GetSpecialFollowingsRequest(long userId, long tagId)
    {
        mid = userId;
        tagid = tagId;
    }

    public long mid { get; set; }

    /// <summary>
    /// TagId
    /// </summary>
    /// <sample>-10:特别关注</sample>
    public long tagid { get; set; } = -10;

    public int pn { get; set; } = 1;

    public int ps { get; set; } = 20;

    public string jsonp { get; set; } = "jsonp";

    //public string callback { get; set; }
}
