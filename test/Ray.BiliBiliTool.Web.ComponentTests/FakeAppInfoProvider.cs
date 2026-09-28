using Ray.BiliBiliTool.Web.Services;

namespace Ray.BiliBiliTool.Web.ComponentTests;

/// <summary>
/// 版本展示的替身：组件测试不该依赖真实程序集上烘焙的版本号。
/// </summary>
internal sealed class FakeAppInfoProvider(string appVersion) : IAppInfoProvider
{
    public string AppVersion { get; } = appVersion;
}
