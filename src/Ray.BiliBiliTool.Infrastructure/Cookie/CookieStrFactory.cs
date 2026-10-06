using Microsoft.Extensions.Configuration;
using Ray.BiliBiliTool.Infrastructure.Extensions;

namespace Ray.BiliBiliTool.Infrastructure.Cookie;

public class CookieStrFactory<TCookieInfo>(IConfiguration configuration)
    where TCookieInfo : CookieInfo
{
    private Dictionary<int, Dictionary<string, string>> CookieDictionary => GetCookieDictionary();

    public int Count => CookieDictionary.Count;

    public TCookieInfo GetCookie(int index) => CreateCookie(GetCookieDictionary()[index]);

    public IReadOnlyList<TCookieInfo> GetCookies() =>
        GetCookieDictionary().Values.Select(CreateCookie).ToArray();

    private static TCookieInfo CreateCookie(Dictionary<string, string> dic)
    {
        return (TCookieInfo)Activator.CreateInstance(typeof(TCookieInfo), dic)!
            ?? throw new InvalidOperationException();
    }

    public static TCookieInfo CreateNew(string cookie)
    {
        Dictionary<string, string> dic = CkStrToDictionary(cookie);
        return (TCookieInfo)Activator.CreateInstance(typeof(TCookieInfo), dic)!
            ?? throw new InvalidOperationException();
    }

    #region private

    private Dictionary<int, Dictionary<string, string>> GetCookieDictionary()
    {
        var list = configuration.GetSection("BiliBiliCookies").Get<List<string>>() ?? [];
        return CookeStrListToCookieDic(list);
    }

    private Dictionary<int, Dictionary<string, string>> CookeStrListToCookieDic(List<string> ckList)
    {
        var dic = new Dictionary<int, Dictionary<string, string>>();
        foreach (var cookie in ckList)
        {
            var parsed = CkStrToDictionary(cookie);
            if (parsed.Count > 0)
                dic.Add(dic.Count, parsed);
        }

        return dic;
    }

    private static Dictionary<string, string> CkStrToDictionary(string? ckStr)
    {
        var dic = new Dictionary<string, string>();
        if (string.IsNullOrWhiteSpace(ckStr))
            return dic;

        var ckItemList = ckStr.Split(
            ';',
            StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries
        );
        foreach (var item in ckItemList)
        {
            var separator = item.IndexOf('=');
            if (separator <= 0)
                continue;

            var key = item[..separator].Trim();
            if (key.Length == 0)
                continue;

            var value = item[(separator + 1)..].Trim();
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
