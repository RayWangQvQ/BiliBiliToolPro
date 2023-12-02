using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Configuration.EnvironmentVariables;

namespace Ray.BiliBiliTool.Config
{
    /// <summary>
    /// 自定义的排除空值的环境变量配置源<para></para>
    /// （用于取待默认的<see cref="EnvironmentVariablesConfigurationSource"/>）
    /// </summary>
    public class EnvironmentVariablesExcludeEmptyConfigurationSource : IConfigurationSource
    {
        public string Prefix { get; set; }

        public bool RemoveKeyPrefix { get; set; }

        public IConfigurationProvider Build(IConfigurationBuilder builder)
        {
            return new EnvironmentVariablesExcludeEmptyConfigurationProvider(Prefix, RemoveKeyPrefix);
        }
    }
}
