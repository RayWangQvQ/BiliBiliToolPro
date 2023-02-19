using System.Collections.Generic;
using System.Text.RegularExpressions;

namespace Ray.BiliBiliTool.Infrastructure.Helpers
{
    public class RegexHelper
    {
        public static string QuerySingle(string source, string pattern)
        {
            Regex rg = new Regex(pattern, RegexOptions.Multiline | RegexOptions.Singleline);
            return rg.Match(source).Value;
        }

        public static List<string> QueryMultiple(string source, string pattern)
        {
            Regex rg = new Regex(pattern, RegexOptions.Multiline | RegexOptions.Singleline);
            //Regex rg = new Regex(pattern, RegexOptions.Singleline);

            MatchCollection matches = rg.Matches(source);

            List<string> resList = new List<string>();

            foreach (Match item in matches)
                resList.Add(item.Value);

            return resList;
        }

        /// <summary>
        /// 截取字符串中开始和结束字符串中间的字符串
        /// </summary>
        /// <param name="source">源字符串</param>
        /// <param name="startStr">开始字符串</param>
        /// <param name="endStr">结束字符串</param>
        /// <returns>中间字符串</returns>
        public static string SubstringSingle(string source, string startStr, string endStr)
        {
            var regexStr = $"(?<=({startStr}))[.\\s\\S]*?(?=({endStr}))";
            Regex rg = new Regex(regexStr, RegexOptions.Multiline | RegexOptions.Singleline);
            return rg.Match(source).Value;
        }

        /// <summary>
        /// （批量）截取字符串中开始和结束字符串中间的字符串
        /// </summary>
        /// <param name="source">源字符串</param>
        /// <param name="startStr">开始字符串</param>
        /// <param name="endStr">结束字符串</param>
        /// <returns>中间字符串</returns>
        public static List<string> SubstringMultiple(string source, string startStr, string endStr)
        {
            var regexStr = $"(?<=({startStr}))[.\\s\\S]*?(?=({endStr}))";
            // Regex rg = new Regex(regexStr, RegexOptions.Multiline | RegexOptions.Singleline);
            Regex rg = new Regex(regexStr, RegexOptions.Singleline);

            MatchCollection matches = rg.Matches(source);

            List<string> resList = new List<string>();

            foreach (Match item in matches)
                resList.Add(item.Value);

            return resList;
        }
    }
}
