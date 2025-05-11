using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Extensions.Configuration;

namespace Ray.BiliBiliTool.Infrastructure.Cookie;

public class CookieStrFactory<TCookieInfo>(IConfiguration configuration)
    where TCookieInfo : CookieInfo
{
    private Dictionary<int, Dictionary<string, string>> CookieDictionary => GetCookieDictionary();

    public int CurrentNum { get; set; } = 1;

    public int Count => CookieDictionary.Count;

    public bool Any()
    {
        if (CurrentNum <= Count)
            return true;
        else
            return false;
    }

    public TCookieInfo GetCurrentCookie()
    {
        Dictionary<string, string> dic;
        if (!Any())
        {
            dic = new Dictionary<string, string>(); //todo
            //throw new Exception($"第 {CurrentNum} 个cookie不存在");
        }

        dic = GetCookieDictionary()[CurrentNum];
        return (TCookieInfo)Activator.CreateInstance(typeof(TCookieInfo), dic);
    }

    public TCookieInfo CreateNew(string cookie)
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
        Dictionary<int, Dictionary<string, string>> dic =
            new Dictionary<int, Dictionary<string, string>>();
        ckList ??= new List<string>();

        for (int i = 0; i < ckList?.Count; i++)
        {
            dic.Add(i + 1, CkStrToDictionary(ckList[i]));
        }

        return dic;
    }

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
