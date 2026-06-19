namespace Ray.BiliBiliTool.Config.Options;

/// <summary>
/// 呆呆面板（Daidai Panel）Open API 配置。
/// 在面板「系统设置 -> Open API」新建应用（授权范围至少含 envs），
/// 拿到 AppKey / AppSecret 后配置到环境变量 DaiDaiConfig__AppKey、DaiDaiConfig__AppSecret，
/// 面板地址通过环境变量 DaiDai_URL 配置（默认 http://127.0.0.1:5700）。
/// </summary>
public class DaiDaiOptions
{
    public string AppKey { get; set; } = "";

    public string AppSecret { get; set; } = "";
}
