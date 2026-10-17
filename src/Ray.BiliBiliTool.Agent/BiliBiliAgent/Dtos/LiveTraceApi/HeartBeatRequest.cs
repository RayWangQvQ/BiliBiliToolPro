using Newtonsoft.Json;
using Ray.BiliBiliTool.Agent.BiliBiliAgent.Services;
using Ray.BiliBiliTool.Agent.BiliBiliAgent.Utils;
using Refit;

namespace Ray.BiliBiliTool.Agent.BiliBiliAgent.Dtos.LiveTraceApi;

public class HeartBeatRequest : IWrid
{
    public HeartBeatRequest(
        long roomId,
        long parentId,
        long areaID,
        int seqNumber, // 心跳包编号
        string buvid, // cookie['LIVE_BUVID']
        long timestamp,
        long ets, // 由后端返回的 timestamp
        string userAgent,
        ICollection<int> secretRule,
        string secretKey,
        string csrf,
        string uuid,
        string device,
        int heartbeatInterval = 60,
        long anchorId = 0
    )
    {
        Id = JsonConvert.SerializeObject(new[] { parentId, areaID, seqNumber, roomId });
        Ets = ets;
        Benchmark = secretKey;
        Time = heartbeatInterval;
        Ts = timestamp;
        Ua = userAgent;
        Csrf = csrf;
        Device = device;
        Ruid = anchorId;

        // Build the heartbeat signature.
        var json = new
        {
            platform = "web",
            parent_id = parentId,
            area_id = areaID,
            seq_id = seqNumber,
            room_id = roomId,
            buvid,
            uuid,
            ets,
            time = heartbeatInterval,
            ts = timestamp,
        };
        string jsonString = JsonConvert.SerializeObject(json);
        S = LiveHeartBeatCrypto.Sypder(jsonString, secretRule, secretKey);

        Visit_id = "";
    }

    [AliasAs("s")]
    public string S { get; set; }

    [AliasAs("id")]
    public string Id { get; set; }

    [AliasAs("ets")]
    public long Ets { get; set; }

    [AliasAs("benchmark")]
    public string Benchmark { get; set; }

    [AliasAs("time")]
    public long Time { get; set; }

    [AliasAs("ts")]
    public long Ts { get; set; }

    [AliasAs("ua")]
    public string Ua { get; set; }

    [AliasAs("csrf_token")]
    public string Csrf_token => Csrf;

    [AliasAs("csrf")]
    public string Csrf { get; set; }

    [AliasAs("visit_id")]
    public string Visit_id { get; set; }

    [AliasAs("device")]
    public string Device { get; }

    [AliasAs("ruid")]
    public long Ruid { get; set; }

    [AliasAs("trackid")]
    public string Trackid { get; set; } = "-99998";

    [AliasAs("web_location")]
    public string Web_location { get; set; } = "444.8";
    public long wts { get; set; }
    public string? w_rid { get; set; }
}
