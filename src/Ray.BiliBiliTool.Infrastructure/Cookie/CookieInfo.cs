using System;
using System.Collections.Generic;
using System.Linq;

namespace Ray.BiliBiliTool.Infrastructure.Cookie;

public class CookieInfo(Dictionary<string, string> cookieDic)
{
    public Dictionary<string, string> CookieItemDictionary { get; private set; } = cookieDic;

    public string CookieStr =>
        string.Join(
            "; ",
            CookieItemDictionary
                .Select(item => $"{CkNameBuild(item.Key)}={CkValueBuild(item.Value)}")
                .ToList()
        );

    public virtual void Check()
    {
        if (CookieItemDictionary == null || CookieItemDictionary.Count == 0)
            throw new Exception("Cookie字符串为空");
    }

    protected virtual string CkNameBuild(string name)
    {
        return name;
    }

    protected virtual string CkValueBuild(string value)
    {
        return value;
    }

    public override string ToString()
    {
        var list = CookieItemDictionary.Select(d =>
            $"{CkNameBuild(d.Key)}={CkValueBuild(d.Value)}"
        );
        return string.Join("; ", list);
    }

    #region merge

    public void MergeCurrentCookieBySetCookieHeaders(IEnumerable<string> setCookieList)
    {
        MergeCurrentCookie(ConvertSetCkHeadersToCkItemList(setCookieList));
    }

    public void MergeCurrentCookie(string ckStr)
    {
        MergeCurrentCookie(ConvertCkStrToCkItemList(ckStr));
    }

    public void MergeCurrentCookie(List<string> ckItemList)
    {
        MergeCurrentCookie(ConvertCkItemListToCkDic(ckItemList));
    }

    public void MergeCurrentCookie(Dictionary<string, string> ckDic)
    {
        foreach (var item in ckDic)
        {
            CookieItemDictionary[item.Key] = item.Value;
        }
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
    public static Dictionary<string, string> ConvertCkItemListToCkDic(
        IEnumerable<string> ckItemList
    )
    {
        return ckItemList.ToDictionary(
            k => k[..k.IndexOf("=", StringComparison.Ordinal)].Trim(),
            v => v[(v.IndexOf("=", StringComparison.Ordinal) + 1)..].Trim().TrimEnd(';')
        );
    }

    #endregion
}
