namespace Ray.BiliBiliTool.Web.Services;

/// <summary>
/// 面板里展示的宿主元信息。目前只有「应用版本」，将来要加运行时信息（.NET 版本、启动时间等）也走这里。
/// </summary>
public interface IAppInfoProvider
{
    /// <summary>
    /// 应用版本：CI 产物是版本号（如 4.0.8-alpha.3），非 CI 产物是兜底文案。
    /// </summary>
    string AppVersion { get; }
}
