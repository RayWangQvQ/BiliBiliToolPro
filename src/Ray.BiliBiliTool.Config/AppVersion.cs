using System.Reflection;

namespace Ray.BiliBiliTool.Config;

public static class AppVersion
{
    /// <summary>
    /// 非 CI 产物的版本串：与 common.props 的 VersionPrefix/VersionSuffix 保持一致（ADR-0002）。
    /// 本地构建、本地 docker build 不传 VERSION 时，产物恒为它。
    /// </summary>
    public const string LocalBuildVersion = "0.0.0-dev";

    /// <summary>
    /// 非 CI 产物的统一展示文案。版本号读不到（属性缺失）与本地构建对外是同一件事：都不是 CI 产物。
    /// </summary>
    public const string LocalBuildDisplay = "开发版";

    /// <summary>
    /// 取用于展示的完整版本号（如 4.0.3-alpha.1）。
    /// CLR 的数字 AssemblyVersion 装不下 -alpha.N 这类预发布后缀，只有 InformationalVersion 能，
    /// 所以日志必须读它才能和镜像 tag、GitHub Release 一致；尾部附加的 +commit 对用户无意义，截掉。
    /// </summary>
    public static string? InformationalOf(Assembly assembly) =>
        assembly
            .GetCustomAttribute<AssemblyInformationalVersionAttribute>()
            ?.InformationalVersion.Split('+')[0];

    /// <summary>
    /// 取可直接上界面/进日志的「应用版本」。非 CI 产物一律折叠为 <see cref="LocalBuildDisplay"/>，
    /// 避免把 0.0.0-dev 这种占位串当成真版本号显示出去。
    /// </summary>
    public static string DisplayOf(Assembly assembly)
    {
        var informational = InformationalOf(assembly);
        return IsLocalBuild(informational) ? LocalBuildDisplay : informational!;
    }

    /// <summary>
    /// 是否非 CI 产物：版本串为空、或就是本地构建兜底值。
    /// </summary>
    public static bool IsLocalBuild(string? informationalVersion) =>
        string.IsNullOrWhiteSpace(informationalVersion)
        || informationalVersion == LocalBuildVersion;
}
