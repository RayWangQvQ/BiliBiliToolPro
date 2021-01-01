using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Extensions.Configuration;
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

        public EnvironmentVariablesExcludeEmptyConfigurationProvider(string prefix = null) : base(prefix)
        {
            _prefix = prefix ?? string.Empty;

            _startsWith = c => c.Key.StartsWith(prefix, StringComparison.OrdinalIgnoreCase);
            _removeNullValue = c => !string.IsNullOrWhiteSpace(c.Value);
            _fifter = c => _startsWith(c) && _removeNullValue(c);
        }

        public override void Load()
        {
            Dictionary<string, string> dictionary = Environment.GetEnvironmentVariables()
                .ToDictionary(otherAction: t => t
                     .Where(_fifter)
                     .Select(x => x.NewKey(key => NormalizeKey(key))));

            base.Data = new Dictionary<string, string>(dictionary, StringComparer.OrdinalIgnoreCase);
        }

        /// <summary>
        /// 格式化Key
        /// </summary>
        /// <param name="key"></param>
        /// <returns></returns>
        private string NormalizeKey(string key)
        {
            key = RemoveKeyPrefix(key);
            key = ReplaceKeyDelimiter(key);
            return key;
        }

        /// <summary>
        /// 移除指定前缀
        /// </summary>
        /// <param name="key"></param>
        /// <returns></returns>
        private string RemoveKeyPrefix(string key)
        {
            return _prefix.IsNullOrEmpty()
                ? key
                : key.Substring(_prefix.Length);
        }

        /// <summary>
        /// 替换分隔符
        /// </summary>
        /// <param name="key"></param>
        /// <returns></returns>
        private string ReplaceKeyDelimiter(string key)
        {
            return key.Replace("__", ConfigurationPath.KeyDelimiter);
        }
    }
}
