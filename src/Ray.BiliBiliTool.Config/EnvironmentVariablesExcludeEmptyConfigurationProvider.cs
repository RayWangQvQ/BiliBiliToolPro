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
        private readonly string _prefix;
        private readonly Func<KeyValuePair<string, string>, bool> _startsWith;
        private readonly Func<KeyValuePair<string, string>, bool> _removeNullValue;
        private readonly Func<KeyValuePair<string, string>, bool> _fifter;
        private readonly Func<KeyValuePair<string, string>, KeyValuePair<string, string>> _removePrefix;

        public EnvironmentVariablesExcludeEmptyConfigurationProvider(string prefix = null) : base(prefix)
        {
            _prefix = prefix ?? string.Empty;
            _startsWith = c => c.Key.StartsWith(prefix, StringComparison.OrdinalIgnoreCase);
            _removeNullValue = c => !string.IsNullOrWhiteSpace(c.Value);
            _fifter = c => _startsWith(c) && _removeNullValue(c);

            _removePrefix = prefix.Length == 0
                ? t => t
                : t => t.NewKey(c => c.Substring(prefix.Length));
        }

        public override void Load()
        {
            Dictionary<string, string> dictionary = Environment.GetEnvironmentVariables()
                .ToDictionary(otherAction: t => t
                     .Where(_fifter)
                     .Select(_removePrefix));

            base.Data = new Dictionary<string, string>(dictionary, StringComparer.OrdinalIgnoreCase);
        }
    }
}
