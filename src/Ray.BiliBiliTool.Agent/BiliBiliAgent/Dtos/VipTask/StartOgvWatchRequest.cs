namespace Ray.BiliBiliTool.Agent.BiliBiliAgent.Dtos.VipTask;

public class StartOgvWatchRequest: BaseAppRequest
{
    public long ep_id { get; } = 328482;

    public long season_id { get; } = 12548;

    public string Activity_code { get; } = "";

    public string spmid { get; } = "united.player-video-detail.0.0";
}