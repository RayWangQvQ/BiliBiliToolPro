using System;
using System.Collections.Generic;
using System.Linq;

namespace Ray.BiliBiliTool.Infrastructure.Cookie
{
    public class CookieInfo
    {
        private readonly CookieStrFactory _ckFactory;

        public CookieInfo(CookieStrFactory ckFactory)
        {
            _ckFactory = ckFactory;
        }

        public string CookieStr => _ckFactory.GetCurrentCookieStr();

        public Dictionary<string, string> CookieItemDictionary => _ckFactory.GetCurrentCookieDic();

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
            _ckFactory.MergeCurrentCookieBySetCookieHeaders(setCookieList);
        }

        public void MergeCurrentCookie(string ckStr)
        {
            _ckFactory.MergeCurrentCookie(ckStr);
        }

        public void MergeCurrentCookie(List<string> ckItemList)
        {
            _ckFactory.MergeCurrentCookie(ckItemList);
        }

        public void MergeCurrentCookie(Dictionary<string, string> ckDic)
        {
            _ckFactory.MergeCurrentCookie(ckDic);
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

    }
}
