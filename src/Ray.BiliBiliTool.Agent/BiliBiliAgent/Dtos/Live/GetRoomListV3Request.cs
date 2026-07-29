namespace Ray.BiliBiliTool.Agent.BiliBiliAgent.Dtos.Live;

/// <summary>
/// room/v3/area/getRoomList 请求（无需 WBI 签名）
/// </summary>
public class GetRoomListV3Request
{
    public string platform { get; set; } = "web";
    public long parent_area_id { get; set; }
    public long area_id { get; set; }
    public string? sort_type { get; set; } = "online";
    public int page { get; set; }
    public int page_size { get; set; } = 99;
}
