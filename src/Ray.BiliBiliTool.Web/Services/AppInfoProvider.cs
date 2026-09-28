using System.Reflection;

namespace Ray.BiliBiliTool.Web.Services;

/// <summary>
/// 读宿主程序集上由 CI 烘焙进去的版本元数据（ADR-0002、ADR-0003），不做任何缓存以外的加工。
/// </summary>
public class AppInfoProvider : IAppInfoProvider
{
    private readonly Assembly _assembly;

    public AppInfoProvider()
        : this(Assembly.GetEntryAssembly() ?? typeof(AppInfoProvider).Assembly) { }

    public AppInfoProvider(Assembly assembly)
    {
        _assembly = assembly;
    }

    public string AppVersion => Config.AppVersion.DisplayOf(_assembly);
}
