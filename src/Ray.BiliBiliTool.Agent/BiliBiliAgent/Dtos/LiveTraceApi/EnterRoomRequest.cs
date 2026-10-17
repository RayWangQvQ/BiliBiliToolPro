using Newtonsoft.Json;
using Ray.BiliBiliTool.Agent.BiliBiliAgent.Services;
using Refit;

namespace Ray.BiliBiliTool.Agent.BiliBiliAgent.Dtos.LiveTraceApi;

public class EnterRoomRequest : IWrid
{
    public EnterRoomRequest(
        long roomId,
        long parentId,
        long areaID,
        int seqNumber, // 心跳包编号
        long timestamp,
        string userAgent,
        string csrf,
        long ruid,
        string device
    )
    {
        Id = JsonConvert.SerializeObject(new[] { parentId, areaID, seqNumber, roomId });
        Ts = timestamp;
        Ua = userAgent;
        Csrf = csrf;
        Ruid = ruid;

        Is_patch = 0;
        Heart_beat = "[]";
        Visit_id = "";
        Device = device;
    }

    [AliasAs("id")]
    public string Id { get; set; }

    [AliasAs("ruid")]
    public long Ruid { get; set; }

    [AliasAs("ts")]
    public long Ts { get; set; }

    [AliasAs("is_patch")]
    public int Is_patch { get; set; }

    [AliasAs("heart_beat")]
    public string Heart_beat { get; set; }

    [AliasAs("ua")]
    public string Ua { get; set; }

    [AliasAs("csrf_token")]
    public string Csrf_token => Csrf;

    [AliasAs("csrf")]
    public string Csrf { get; set; }

    [AliasAs("visit_id")]
    public string Visit_id { get; set; }

    [AliasAs("device")]
    public string Device { get; set; }

    [AliasAs("web_location")]
    public string Web_location { get; set; } = "444.8";
    public long wts { get; set; }
    public string? w_rid { get; set; }
}
