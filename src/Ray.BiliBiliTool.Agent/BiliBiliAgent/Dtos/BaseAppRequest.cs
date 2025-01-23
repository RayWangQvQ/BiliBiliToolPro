namespace Ray.BiliBiliTool.Agent.BiliBiliAgent.Dtos;

public class BaseAppRequest
{
    // public string access_key { get; set; }
    // public string appkey { get; set; }

    public string build { get; } = "7720200";

    public string c_locale { get; } = "zh_CN";

    public string channel { get; } = Constants.Channel;

    public int disable_rcmd { get; } = 0;

    public string from_spmid { get; } = "united.player-video-detail.player.continue";

    public string mobi_app { get; } = "android";

    public string platform { get; } = "android";

    public string s_locale { get; } = "zh_CN";

    public string statistics { get; } = "{\"appId\":1,\"platform\":3,\"version\":\"7.72.0\",\"abtest\":\"\"}";

    // public long ts { get; set; }

    public string sign { get; set; }
}