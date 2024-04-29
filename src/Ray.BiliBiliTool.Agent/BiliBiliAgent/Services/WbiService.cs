using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text.RegularExpressions;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Ray.BiliBiliTool.Agent.BiliBiliAgent.Dtos;
using Ray.BiliBiliTool.Agent.BiliBiliAgent.Interfaces;
using Ray.BiliBiliTool.Infrastructure.Helpers;

namespace Ray.BiliBiliTool.Agent.BiliBiliAgent.Services;

/// <summary>
/// 防爬
/// </summary>
public class WbiService : IWbiService
{
    private readonly ILogger<WbiService> _logger;
    private readonly IUserInfoApi _userInfoApi;

    public WbiService(
        ILogger<WbiService> logger,
        IUserInfoApi userInfoApi
    )
    {
        _logger = logger;
        _userInfoApi = userInfoApi;
    }

    public async Task SetWridAsync<T>(T request) where T: IWrid
    {
        //生成字典
        Dictionary<string, object> parameters = ObjectHelper.ObjectToDictionary(request);
        parameters.Remove(nameof(IWrid.wts));
        parameters.Remove(nameof(IWrid.w_rid));

        //根据当前用户信息取加密key
        WbiImg wbi = await GetWbiKeysAsync();

        //生成
        var re = EncWbi(parameters, wbi.ImgKey, wbi.SubKey);

        request.w_rid = re.w_rid;
        request.wts = re.wts;
    }

    /// <summary>
    /// 为请求参数进行 wbi 签名
    /// </summary>
    /// <param name="parameters"></param>
    /// <param name="imgKey"></param>
    /// <param name="subKey"></param>
    /// <returns></returns>
    public WridDto EncWbi(Dictionary<string, object> parameters, string imgKey, string subKey, long timespan = 0)
    {
        var re = new WridDto();

        var mixinKey = GetMixinKey(imgKey + subKey);

        if (timespan == 0)
        {
            re.wts = (long)(DateTime.UtcNow - new DateTime(1970, 1, 1)).TotalSeconds;
        }
        else
        {
            re.wts = timespan;
        }

        var chrFilter = new Regex("[!'()*]");

        var dic = new Dictionary<string, string>
        {
            { "wts", re.wts.ToString() }
        };

        foreach (var entry in parameters)
        {
            var key = entry.Key;
            var value = entry.Value.ToString();

            var encodedValue = chrFilter.Replace(value, "");

            dic.Add(Uri.EscapeDataString(key), Uri.EscapeDataString(encodedValue));
        }

        var keyList = dic.Keys.ToList();
        keyList.Sort();

        var queryList = new List<string>();
        foreach (var item in keyList)
        {
            var value = dic[item];
            queryList.Add($"{item}={value}");
        }

        var queryString = string.Join("&", queryList);
        var md5Hasher = MD5.Create();
        var hashStr = queryString + mixinKey;
        var hashedQueryString = md5Hasher.ComputeHash(Encoding.UTF8.GetBytes(hashStr));
        var wbiSign = BitConverter.ToString(hashedQueryString).Replace("-", "").ToLower();

        re.w_rid = wbiSign;

        return re;
    }


    private async Task<WbiImg> GetWbiKeysAsync()
    {
        BiliApiResponse<UserInfo> apiResponse = await _userInfoApi.LoginByCookie();

        UserInfo useInfo = apiResponse.Data;

        _logger.LogDebug("【img_url】{0}", useInfo.Wbi_img?.img_url);
        _logger.LogDebug("【sub_url】{0}", useInfo.Wbi_img?.sub_url);

        return useInfo.Wbi_img;
    }

    /// <summary>
    /// 对 imgKey 和 subKey 进行字符顺序打乱编码
    /// </summary>
    /// <param name="orig"></param>
    /// <returns></returns>
    private string GetMixinKey(string orig)
    {
        int[] mixinKeyEncTab = new int[]
        {
            46, 47, 18, 2, 53, 8, 23, 32, 15, 50, 10, 31, 58, 3, 45, 35, 27, 43, 5, 49,
            33, 9, 42, 19, 29, 28, 14, 39,12, 38, 41, 13, 37, 48, 7, 16, 24, 55, 40,
            61, 26, 17, 0, 1, 60, 51, 30, 4, 22, 25, 54, 21, 56, 59, 6, 63, 57, 62, 11,
            36, 20, 34, 44, 52
        };

        var temp = new StringBuilder();
        foreach (var index in mixinKeyEncTab)
        {
            temp.Append(orig[index]);
        }
        return temp.ToString().Substring(0, 32);
    }
}

public class WridDto: IWrid
{
    public long wts { get; set; }

    public string w_rid { get; set; }
}

public interface IWrid
{
    public long wts { get; set; }

    public string w_rid { get; set; }
}
