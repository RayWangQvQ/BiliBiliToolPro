using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
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
        private readonly Func<KeyValuePair<string, string>, bool> startsWith;
        private readonly Func<KeyValuePair<string, string>, bool> removeNullValue;
        private readonly Func<KeyValuePair<string, string>, bool> fifter;
        private readonly Func<KeyValuePair<string, string>, KeyValuePair<string, string>> removePrefix;

        public EnvironmentVariablesExcludeEmptyConfigurationProvider(string prefix = null) : base(prefix)
        {
            this.prefix = prefix ?? string.Empty;
            this.startsWith = c => c.Key.StartsWith(this.prefix, StringComparison.OrdinalIgnoreCase);
            this.removeNullValue = c => !string.IsNullOrWhiteSpace(c.Value);
            this.fifter = c => this.startsWith(c) && this.removeNullValue(c);

            this.removePrefix = this.prefix.Length == 0
                ? t => t
                : t => t.NewKey(c => c.Substring(this.prefix.Length));
        }

        public override void Load()
        {
            Dictionary<string, string> dictionary = Environment.GetEnvironmentVariables()
                .ToDictionary(otherAction: t => t
                     .Where(this.fifter)
                     .Select(this.removePrefix));

            base.Data = new Dictionary<string, string>(dictionary, StringComparer.OrdinalIgnoreCase);
        }
    }
}
