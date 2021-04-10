using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Ray.BiliBiliTool.Agent;
using Ray.BiliBiliTool.Agent.BiliBiliAgent.Dtos;
using Ray.BiliBiliTool.Agent.BiliBiliAgent.Dtos.Relation;
using Ray.BiliBiliTool.Agent.BiliBiliAgent.Interfaces;
using Ray.BiliBiliTool.Config;
using Ray.BiliBiliTool.DomainService.Interfaces;

namespace Ray.BiliBiliTool.DomainService
{
    /// <summary>
    /// 账户
    /// </summary>
    public class AccountDomainService : IAccountDomainService
    {
        private readonly ILogger<AccountDomainService> _logger;
        private readonly IDailyTaskApi _dailyTaskApi;
        private readonly IUserInfoApi _userInfoApi;
        private readonly IRelationApi _relationApi;
        private readonly BiliCookie _cookie;

        public AccountDomainService(
            ILogger<AccountDomainService> logger,
            IDailyTaskApi dailyTaskApi,
            BiliCookie cookie,
            IUserInfoApi userInfoApi,
            IRelationApi relationApi
        )
        {
            _logger = logger;
            _dailyTaskApi = dailyTaskApi;
            _cookie = cookie;
            _userInfoApi = userInfoApi;
            _relationApi = relationApi;
        }

        /// <summary>
        /// 登录
        /// </summary>
        /// <returns></returns>
        public UserInfo LoginByCookie()
        {
            BiliApiResponse<UserInfo> apiResponse = _userInfoApi.LoginByCookie().GetAwaiter().GetResult();

            if (apiResponse.Code != 0 || !apiResponse.Data.IsLogin)
            {
                _logger.LogWarning("登录异常，请检查Cookie是否错误或过期");
                return null;
            }

            UserInfo useInfo = apiResponse.Data;

            //获取到UserId
            _cookie.UserId = useInfo.Mid.ToString();

            _logger.LogInformation("【用户名】 {0}", useInfo.GetFuzzyUname());
            _logger.LogInformation("【硬币余额】 {0}", useInfo.Money ?? 0);

            if (useInfo.Level_info.Current_level < 6)
            {
                _logger.LogInformation("【距升级 Lv{0}】 {1}天（如每日做满65点经验）",
                    useInfo.Level_info.Current_level + 1,
                    (useInfo.Level_info.GetNext_expLong() - useInfo.Level_info.Current_exp) / Constants.EveryDayExp);
            }
            else
            {
                _logger.LogInformation("【当前经验】{0} （您已是 Lv6 的大佬了，无敌是多么寂寞~）", useInfo.Level_info.Current_exp);
            }

            return useInfo;
        }

        /// <summary>
        /// 获取每日任务完成情况
        /// </summary>
        /// <returns></returns>
        public DailyTaskInfo GetDailyTaskStatus()
        {
            DailyTaskInfo result = new();
            BiliApiResponse<DailyTaskInfo> apiResponse = _dailyTaskApi.GetDailyTaskRewardInfo().GetAwaiter().GetResult();
            if (apiResponse.Code == 0)
            {
                _logger.LogDebug("请求本日任务完成状态成功");
                result = apiResponse.Data;
            }
            else
            {
                _logger.LogWarning("获取今日任务完成状态失败：{result}", apiResponse.ToJson());
                result = _dailyTaskApi.GetDailyTaskRewardInfo().GetAwaiter().GetResult().Data;
                //todo:偶发性请求失败，再请求一次，这么写很丑陋，待用polly再框架层面实现
            }

            return result;
        }

        /// <summary>
        /// 取关
        /// </summary>
        /// <param name="groupName"></param>
        /// <param name="count"></param>
        public void UnfollowBatched(string groupName, int count)
        {
            _logger.LogInformation("【分组名】{group}", groupName);

            //根据分组名称获取tag
            TagDto tag = GetTag(groupName);
            int? tagId = tag?.Tagid;
            int total = tag?.Count ?? 0;

            if (!tagId.HasValue)
            {
                _logger.LogWarning("分组名称不存在");
                return;
            }

            if (total == 0)
            {
                _logger.LogWarning("分组下不存在up");
                return;
            }

            if (count == -1) count = total;

            _logger.LogInformation("【分组下共有】{count}人", total);
            _logger.LogInformation("【目标取关】{count}人" + Environment.NewLine, count);

            //计算共几页
            int totalPage = (int)Math.Ceiling(total / (double)20);

            //从最后一页开始获取
            var req = new GetSpecialFollowingsRequest(long.Parse(_cookie.UserId), tagId.Value)
            {
                Pn = totalPage
            };
            List<UpInfo> followings = _relationApi.GetFollowingsByTag(req)
                .GetAwaiter().GetResult()
                .Data;
            followings.Reverse();

            var targetList = new List<UpInfo>();

            if (count <= followings.Count)
            {
                targetList = followings.Take(count).ToList();
            }
            else
            {
                int pn = totalPage;
                while (targetList.Count < count)
                {
                    targetList.AddRange(followings);

                    //获取前一页
                    pn -= 1;
                    if (pn <= 0) break;
                    req.Pn = pn;
                    followings = _relationApi.GetFollowingsByTag(req)
                        .GetAwaiter().GetResult()
                        .Data;
                    followings.Reverse();
                }
            }

            _logger.LogInformation("开始取关..." + Environment.NewLine);
            int success = 0;
            for (int i = 1; i <= targetList.Count && i <= count; i++)
            {
                UpInfo info = targetList[i - 1];

                _logger.LogInformation("【序号】{num}", i);
                _logger.LogInformation("【UP】{up}", info.Uname);

                string modifyReferer = string.Format(RelationApiConstant.ModifyReferer, _cookie.UserId, tagId);
                var modifyReq = new ModifyRelationRequest(info.Mid, _cookie.BiliJct);
                var re = _relationApi.ModifyRelation(modifyReq, modifyReferer)
                    .GetAwaiter().GetResult();

                if (re.Code == 0)
                {
                    _logger.LogInformation("【取关结果】成功" + Environment.NewLine);
                    success++;
                }
                else
                {
                    _logger.LogInformation("【取关结果】失败");
                    _logger.LogInformation("【原因】{msg}" + Environment.NewLine, re.Message);
                }
            }

            _logger.LogInformation("【本次共取关】{count}人", success);

            //计算剩余
            tag = GetTag(groupName);
            _logger.LogInformation("【分组下剩余】{count}人", tag?.Count ?? 0);
        }

        /// <summary>
        /// 获取分组（标签）
        /// </summary>
        /// <param name="groupName"></param>
        /// <returns></returns>
        private TagDto GetTag(string groupName)
        {
            string getTagsReferer = string.Format(RelationApiConstant.GetTagsReferer, _cookie.UserId);
            List<TagDto> tagList = _relationApi.GetTags(getTagsReferer)
                .GetAwaiter().GetResult()
                .Data;
            TagDto tag = tagList.FirstOrDefault(x => x.Name == groupName);
            return tag;
        }
    }
}
