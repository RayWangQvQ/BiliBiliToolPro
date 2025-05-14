using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Extensions.Configuration;

namespace Ray.BiliBiliTool.Infrastructure.Cookie;

public class CookieStrFactory<TCookieInfo>(IConfiguration configuration)
    where TCookieInfo : CookieInfo
{
    private Dictionary<int, Dictionary<string, string>> CookieDictionary => GetCookieDictionary();

    public int Count => CookieDictionary.Count;

    public TCookieInfo GetCookie(int index)
    {
        var dic = GetCookieDictionary()[index];
        return (TCookieInfo)Activator.CreateInstance(typeof(TCookieInfo), dic);
    }

    public static TCookieInfo CreateNew(string cookie)
    {
        Dictionary<string, string> dic = CkStrToDictionary(cookie);
        return (TCookieInfo)Activator.CreateInstance(typeof(TCookieInfo), dic);
    }

    #region private

    private Dictionary<int, Dictionary<string, string>> GetCookieDictionary()
    {
        var list = configuration.GetSection("BiliBiliCookies").Get<List<string>>();
        return CookeStrListToCookieDic(list);
    }

    private Dictionary<int, Dictionary<string, string>> CookeStrListToCookieDic(List<string> ckList)
    {
        var dic = new Dictionary<int, Dictionary<string, string>>();
        ckList ??= [];

        for (int i = 0; i < ckList?.Count; i++)
        {
            dic.Add(i, CkStrToDictionary(ckList[i]));
        }

        return dic;
    }

    private static Dictionary<string, string> CkStrToDictionary(string ckStr)
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
