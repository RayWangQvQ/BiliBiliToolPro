using System;
using System.Collections.Generic;
using System.Linq;

namespace Ray.BiliBiliTool.Infrastructure.Cookie
{
    public class CookieStrFactory
    {
        private readonly Dictionary<int, Dictionary<string, string>> _cookieDictionary;

        public CookieStrFactory(List<string> strList)
        {
            _cookieDictionary = new Dictionary<int, Dictionary<string, string>>();

            for (int i = 0; i < strList?.Count; i++)
            {
                _cookieDictionary.Add(i + 1, CkStrToDictionary(strList[i]));
            }

            CurrentNum = 1;
        }

        public int CurrentNum { get; set; }

        public int Count => _cookieDictionary.Count;

        public bool Any()
        {
            if (CurrentNum <= Count) return true;
            else return false;
        }

        public Dictionary<string, string> GetCurrentCookieDic()
        {
            if (!Any()) throw new Exception($"第 {CurrentNum} 个cookie不存在");

            return _cookieDictionary[CurrentNum];
        }

        public string GetCurrentCookieStr()
        {
            if (!Any()) throw new Exception($"第 {CurrentNum} 个cookie字符串不存在");

            var ckDic = _cookieDictionary[CurrentNum];
            return DictionaryToCkStr(ckDic);
        }

        #region merge

        /// <summary>
        /// 根据请求返回header中的set-cookie来merge cookie
        /// </summary>
        /// <param name="setCookieList">["ak=av; expire=abc;"]</param>
        public void MergeCurrentCookieBySetCookieHeaders(IEnumerable<string> setCookieList)
        {
            MergeCurrentCookie(CookieInfo.ConvertSetCkHeadersToCkItemList(setCookieList));
        }

        /// <summary>
        /// 根据cookie字符串merge
        /// </summary>
        /// <param name="ckStr"></param>
        public void MergeCurrentCookie(string ckStr)
        {
            MergeCurrentCookie(CookieInfo.ConvertCkStrToCkItemList(ckStr));
        }

        /// <summary>
        /// 根据cookie item集合merge（一个“ak=av”为一个cookie item）
        /// </summary>
        /// <param name="ckItemList">["ak=av","bk=bv"]</param>
        public void MergeCurrentCookie(List<string> ckItemList)
        {
            MergeCurrentCookie(CookieInfo.ConvertCkItemListToCkDic(ckItemList));
        }

        /// <summary>
        /// 根据cookie dic来merge
        /// </summary>
        /// <param name="ckDic">{{"ak":"av"}}</param>
        public void MergeCurrentCookie(Dictionary<string, string> ckDic)
        {
            var currentCkDic = GetCurrentCookieDic();
            foreach (var item in ckDic)
            {
                if (currentCkDic.ContainsKey(item.Key))
                {
                    currentCkDic[item.Key] = item.Value;
                }
                else
                {
                    currentCkDic.Add(item.Key, item.Value);
                }
            }
        }

        #endregion

        #region private

        private Dictionary<string, string> CkStrToDictionary(string ckStr)
        {
            var dic = new Dictionary<string, string>();
            var ckItemList = ckStr.Split(";", StringSplitOptions.TrimEntries).Distinct();
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
