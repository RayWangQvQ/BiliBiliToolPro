using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration.EnvironmentVariables;

namespace Ray.BiliBiliTool.Config
{
    /// <summary>
    /// 自定义的排除空值的环境变量提供者
    /// （使用GitHub Actions的脚本传入环境变量，空值会保留，所以这里自己写了一个用来替换掉默认的<see cref="EnvironmentVariablesConfigurationProvider"/>）
    /// </summary>
    public class EnvironmentVariablesExcludeEmptyConfigurationProvider : EnvironmentVariablesConfigurationProvider
    {
        private readonly string prefix;

        public EnvironmentVariablesExcludeEmptyConfigurationProvider(string prefix = null) : base(prefix)
        {
            this.prefix = prefix ?? string.Empty;
        }

        public override void Load()
        {
            var dictionary = Environment.GetEnvironmentVariables()
                .Cast<DictionaryEntry>()
                .Where(it => it.Key.ToString().StartsWith(prefix, StringComparison.OrdinalIgnoreCase)
                             && !string.IsNullOrWhiteSpace(it.Value.ToString()))//过滤掉空值的
                .ToDictionary(it => it.Key.ToString().Substring(prefix.Length), it => it.Value.ToString());

            this.Data = new Dictionary<string, string>(dictionary, StringComparer.OrdinalIgnoreCase);
        }
    }
}
