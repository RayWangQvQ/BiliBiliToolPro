using System;
using System.Collections.Generic;
using System.Linq;

namespace Ray.BiliBiliTool.Infrastructure.Cookie
{
    public class CookieInfo
    {
        public CookieInfo(string ckStr)
        {
            this.CookieStr = ckStr;
        }

        public string CookieStr { get; set; }

        public Dictionary<string, string> CookieItemDictionary => CkStrToDictionary(CookieStr);

        public virtual void Check()
        {
            if (string.IsNullOrWhiteSpace(CookieStr)) throw new Exception("Cookie字符串为空");
        }

        public virtual string CkNameBuild(string name) { return name; }

        public virtual string CkValueBuild(string value) { return value; }

        public override string ToString()
        {
            var list = CookieItemDictionary.Select(d => $"{CkNameBuild(d.Key)}={CkValueBuild(d.Value)}");
            return string.Join("; ", list);
        }

        #region merge

        public void MergeCurrentCookieBySetCookieHeaders(IEnumerable<string> setCookieList)
        {
            MergeCurrentCookie(CookieInfo.ConvertSetCkHeadersToCkItemList(setCookieList));
        }

        public void MergeCurrentCookie(string ckStr)
        {
            MergeCurrentCookie(CookieInfo.ConvertCkStrToCkItemList(ckStr));
        }

        public void MergeCurrentCookie(List<string> ckItemList)
        {
            MergeCurrentCookie(CookieInfo.ConvertCkItemListToCkDic(ckItemList));
        }

        public void MergeCurrentCookie(Dictionary<string, string> ckDic)
        {
            foreach (var item in ckDic)
            {
                if (CookieItemDictionary.ContainsKey(item.Key))
                {
                    CookieItemDictionary[item.Key] = item.Value;
                }
                else
                {
                    CookieItemDictionary.Add(item.Key, item.Value);
                }
            }

            CookieStr = DictionaryToCkStr(CookieItemDictionary);
        }

        #endregion

        #region convert

        /// <summary>
        /// List<setCkHeader>—>List<ckItem>
        /// </summary>
        /// <param name="setCookieList"></param>
        /// <returns></returns>
        public static List<string> ConvertSetCkHeadersToCkItemList(IEnumerable<string> setCookieList)
        {
            return setCookieList.Select(item => item.Split(';').FirstOrDefault()?.Trim()).ToList();
        }

        /// <summary>
        /// List<setCkHeader>—>ckStr
        /// </summary>
        /// <param name="setCookieList"></param>
        /// <returns></returns>
        public static string ConvertSetCkHeadersToCkStr(IEnumerable<string> setCookieList)
        {
            var ckItemList = ConvertSetCkHeadersToCkItemList(setCookieList);
            return ConvertCkItemListToCkStr(ckItemList);
        }

        /// <summary>
        /// ckStr—>List<ckItem>
        /// </summary>
        /// <param name="ckStr"></param>
        /// <returns></returns>
        public static List<string> ConvertCkStrToCkItemList(string ckStr)
        {
            return ckStr.Split(";", StringSplitOptions.TrimEntries).ToList();
        }

        /// <summary>
        /// List<ckItem>—>ckStr
        /// </summary>
        /// <param name="ckItemList"></param>
        /// <returns></returns>
        public static string ConvertCkItemListToCkStr(IEnumerable<string> ckItemList)
        {
            return string.Join("; ", ckItemList);
        }

        /// <summary>
        /// List<ckItem>—>Dictionary<>
        /// </summary>
        /// <param name="ckItemList"></param>
        /// <returns></returns>
        public static Dictionary<string, string> ConvertCkItemListToCkDic(IEnumerable<string> ckItemList)
        {
            return ckItemList.ToDictionary(k => k[..k.IndexOf("=", StringComparison.Ordinal)].Trim(),
                v => v[(v.IndexOf("=", StringComparison.Ordinal) + 1)..].Trim().TrimEnd(';'));
        }

        #endregion

        #region private

        private Dictionary<string, string> CkStrToDictionary(string ckStr)
        {
            var dic = new Dictionary<string, string>();
            var ckItemList = ckStr
                .Split(";", StringSplitOptions.TrimEntries)
                .Distinct()
                .Where(x=>!string.IsNullOrWhiteSpace(x))
                .ToList();
            foreach (var item in ckItemList)
            {
                var key = item[..item.IndexOf("=", StringComparison.Ordinal)].Trim();
                var value = item[(item.IndexOf("=", StringComparison.Ordinal) + 1)..].Trim();
                dic.AddIfNotExist(new KeyValuePair<string, string>(key, value), p => p.Key == key);
            }
            return dic;
        }

        private string DictionaryToCkStr(Dictionary<string, string> dic)
        {
            var list = dic.Select(item => $"{item.Key}={item.Value}").ToList();
            return string.Join("; ", list);
        }

        #endregion

    }
}
