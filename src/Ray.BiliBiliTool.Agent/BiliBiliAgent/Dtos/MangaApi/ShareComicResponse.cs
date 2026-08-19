using System.Text.Json.Serialization;

namespace Ray.BiliBiliTool.Agent.BiliBiliAgent.Dtos.MangaApi;

/// <summary>
/// 漫画分享响应（activity.v1.Activity/ShareComic），data.point 为本次分享获得的积分
/// </summary>
public class ShareComicResponse
{
    [JsonPropertyName("point")]
    public int Point { get; set; }
}
