using System.Reflection;

namespace Ray.BiliBiliTool.Config;

public static class AppVersion
{
    /// <summary>
    /// 取用于展示的完整版本号（如 4.0.3-alpha.1）。
    /// CLR 的数字 AssemblyVersion 装不下 -alpha.N 这类预发布后缀，只有 InformationalVersion 能，
    /// 所以日志必须读它才能和镜像 tag、GitHub Release 一致；尾部附加的 +commit 对用户无意义，截掉。
    /// </summary>
    public static string? InformationalOf(Assembly assembly) =>
        assembly
            .GetCustomAttribute<AssemblyInformationalVersionAttribute>()
            ?.InformationalVersion.Split('+')[0];
}
