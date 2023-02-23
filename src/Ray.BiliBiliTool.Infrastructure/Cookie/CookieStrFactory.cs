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
        }

        public int CurrentNum { get; set; } = 1;

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

        public void MergeCurrentCookie(string ckStr)
        {
            MergeCurrentCookie(ckStr.Split(";", StringSplitOptions.TrimEntries).ToList());
        }

        public void MergeCurrentCookie(List<string> ckItemList)
        {
            MergeCurrentCookie(ckItemList.ToDictionary(k => k[..(k.IndexOf("=", StringComparison.Ordinal) + 1)],
                v => v[v.IndexOf("=", StringComparison.Ordinal)..]));
        }

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
            return ckStr.Split(";", StringSplitOptions.TrimEntries)
                .ToDictionary(k => k[..k.IndexOf("=", StringComparison.Ordinal)].Trim(),
                    v => v[(v.IndexOf("=", StringComparison.Ordinal) + 1)..].Trim());
        }

        private string DictionaryToCkStr(Dictionary<string, string> dic)
        {
            var list = dic.Select(item => $"{item.Key}={item.Value}").ToList();
            return string.Join("; ", list);
        }

        #endregion

    }
}
