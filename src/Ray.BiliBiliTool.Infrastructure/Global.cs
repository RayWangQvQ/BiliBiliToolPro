using Microsoft.Extensions.Configuration;

namespace Ray.BiliBiliTool.Infrastructure;

public class Global
{
    /// <summary>
    /// 根配置
    /// </summary>
    public static IConfigurationRoot? ConfigurationRoot { get; set; }

    /// <summary>
    /// 根容器
    /// </summary>
    public static IServiceProvider? ServiceProviderRoot { get; set; }
}
