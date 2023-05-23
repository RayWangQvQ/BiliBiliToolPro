using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text.RegularExpressions;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Ray.BiliBiliTool.Agent;
using Ray.BiliBiliTool.Agent.BiliBiliAgent.Dtos;
using Ray.BiliBiliTool.Agent.BiliBiliAgent.Dtos.Relation;
using Ray.BiliBiliTool.Agent.BiliBiliAgent.Interfaces;
using Ray.BiliBiliTool.Config;
using Ray.BiliBiliTool.Config.Options;
using Ray.BiliBiliTool.DomainService.Interfaces;
using System.Reflection;
using Ray.BiliBiliTool.Infrastructure.Helpers;

namespace Ray.BiliBiliTool.DomainService
{
    /// <summary>
    /// 账户
    /// </summary>
    public class WbiDomainService : IWbiDomainService
    {
        private readonly ILogger<AccountDomainService> _logger;
        private readonly IDailyTaskApi _dailyTaskApi;
        private readonly IUserInfoApi _userInfoApi;
        private readonly IRelationApi _relationApi;
        private readonly UnfollowBatchedTaskOptions _unfollowBatchedTaskOptions;
        private readonly BiliCookie _cookie;

        public WbiDomainService(
            ILogger<AccountDomainService> logger,
            IDailyTaskApi dailyTaskApi,
            BiliCookie cookie,
            IUserInfoApi userInfoApi,
            IRelationApi relationApi,
            IOptionsMonitor<UnfollowBatchedTaskOptions> unfollowBatchedTaskOptions
        )
        {
            _logger = logger;
            _dailyTaskApi = dailyTaskApi;
            _cookie = cookie;
            _userInfoApi = userInfoApi;
            _relationApi = relationApi;
            _unfollowBatchedTaskOptions = unfollowBatchedTaskOptions.CurrentValue;
        }

        public async Task<WridDto> GetWridAsync(object ob)
        {
            var parameters = ObjectHelper.ObjectToDictionary(ob);

            var wbi = await GetWbiKeysAsync();

            var re = EncWbi(parameters, wbi.GetImgKey(), wbi.GetSubKey());

            return re;
        }

        public async Task<WbiImg> GetWbiKeysAsync()
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
        public string GetMixinKey(string orig)
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

        /// <summary>
        /// 为请求参数进行 wbi 签名
        /// </summary>
        /// <param name="parameters"></param>
        /// <param name="imgKey"></param>
        /// <param name="subKey"></param>
        /// <returns></returns>
        public WridDto EncWbi(Dictionary<string, object> parameters, string imgKey, string subKey, long timespan=0)
        {
            WridDto re=new WridDto();

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

            var dic=new Dictionary<string, string>();
            dic.Add("wts", re.wts.ToString());

            foreach (var entry in parameters)
            {
                var key = entry.Key;
                var value = entry.Value.ToString();

                var encodedValue = chrFilter.Replace(value, "");

                dic.Add(Uri.EscapeDataString(key), Uri.EscapeDataString(encodedValue));
            }

            var keyList= dic.Keys.ToList();
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
    }

    public class WridDto
    {
        public long wts { get; set; }

        public string w_rid { get; set; }
    }
}
