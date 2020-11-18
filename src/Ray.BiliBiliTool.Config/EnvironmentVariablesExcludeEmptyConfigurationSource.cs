using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Configuration.EnvironmentVariables;

namespace Ray.BiliBiliTool.Config
{
    /// <summary>
    /// 自定义的排除空值的环境变量配置源
    /// （用于取待默认的<see cref="EnvironmentVariablesConfigurationSource"/>）
    /// </summary>
    public class EnvironmentVariablesExcludeEmptyConfigurationSource : IConfigurationSource
    {
        public string Prefix { get; set; }

        public IConfigurationProvider Build(IConfigurationBuilder builder)
        {
            return new EnvironmentVariablesExcludeEmptyConfigurationProvider(this.Prefix);
        }
    }
}
