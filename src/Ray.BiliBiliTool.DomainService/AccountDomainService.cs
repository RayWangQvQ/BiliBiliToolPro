using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Ray.BiliBiliTool.Agent;
using Ray.BiliBiliTool.Agent.BiliBiliAgent.Dtos;
using Ray.BiliBiliTool.Agent.BiliBiliAgent.Dtos.Relation;
using Ray.BiliBiliTool.Agent.BiliBiliAgent.Interfaces;
using Ray.BiliBiliTool.Config.Options;
using Ray.BiliBiliTool.DomainService.Interfaces;
using Ray.BiliBiliTool.Infrastructure.Cookie;

namespace Ray.BiliBiliTool.DomainService;

/// <summary>
/// 账户
/// </summary>
public class AccountDomainService(
    ILogger<AccountDomainService> logger,
    IDailyTaskApi dailyTaskApi,
    IUserInfoApi userInfoApi,
    IRelationApi relationApi,
    IOptionsMonitor<UnfollowBatchedTaskOptions> unfollowBatchedTaskOptions,
    IOptionsMonitor<DailyTaskOptions> dailyTaskOptions,
    CookieStrFactory<BiliCookie> cookieFactory
) : IAccountDomainService
{
    private readonly UnfollowBatchedTaskOptions _unfollowBatchedTaskOptions =
        unfollowBatchedTaskOptions.CurrentValue;
    private readonly DailyTaskOptions _dailyTaskOptions = dailyTaskOptions.CurrentValue;

    /// <summary>
    /// 登录
    /// </summary>
    /// <returns></returns>
    public async Task<UserInfo> LoginByCookie(BiliCookie cookie)
    {
        BiliApiResponse<UserInfo> apiResponse = await userInfoApi.LoginByCookie(cookie.ToString());

        if (apiResponse.Code != 0 || !apiResponse.Data.IsLogin)
        {
            logger.LogWarning("登录异常，请检查Cookie是否错误或过期");
            return null;
        }

        UserInfo useInfo = apiResponse.Data;

        logger.LogInformation("【用户名】{0}", useInfo.GetFuzzyUname());
        logger.LogInformation("【会员类型】{0}", useInfo.VipType.Description());
        logger.LogInformation("【会员状态】{0}", useInfo.VipStatus.Description());
        logger.LogInformation("【硬币余额】{0}", useInfo.Money ?? 0);

        if (useInfo.Level_info.Current_level < 6)
        {
            logger.LogInformation(
                "【距升级Lv{0}】预计{1}天",
                useInfo.Level_info.Current_level + 1,
                CalculateUpgradeTime(useInfo)
            );
        }
        else
        {
            logger.LogInformation("【当前经验】{0}", useInfo.Level_info.Current_exp);
            logger.LogInformation("您已是 Lv6 的大佬了，无敌是多么寂寞~");
        }

        return useInfo;
    }

    /// <summary>
    /// 获取每日任务完成情况
    /// </summary>
    /// <returns></returns>
    public async Task<DailyTaskInfo> GetDailyTaskStatus(BiliCookie ck)
    {
        DailyTaskInfo result = new();
        BiliApiResponse<DailyTaskInfo> apiResponse = await dailyTaskApi.GetDailyTaskRewardInfoAsync(
            ck.ToString()
        );
        if (apiResponse.Code == 0)
        {
            logger.LogDebug("请求本日任务完成状态成功");
            result = apiResponse.Data;
        }
        else
        {
            logger.LogWarning("获取今日任务完成状态失败：{result}", apiResponse.ToJsonStr());
            result = (await dailyTaskApi.GetDailyTaskRewardInfoAsync(ck.ToString())).Data;
            //todo:偶发性请求失败，再请求一次，这么写很丑陋，待用polly再框架层面实现
        }

        return result;
    }

    /// <summary>
    /// 取关
    /// </summary>
    /// <param name="groupName"></param>
    /// <param name="count"></param>
    public async Task UnfollowBatched(BiliCookie ck)
    {
        logger.LogInformation("【分组名】{group}", _unfollowBatchedTaskOptions.GroupName);

        //根据分组名称获取tag
        TagDto tag = await GetTag(_unfollowBatchedTaskOptions.GroupName, ck);
        var tagId = tag?.Tagid;
        int total = tag?.Count ?? 0;

        if (!tagId.HasValue)
        {
            logger.LogWarning("分组名称不存在");
            return;
        }

        if (total == 0)
        {
            logger.LogWarning("分组下不存在up");
            return;
        }
        int count = _unfollowBatchedTaskOptions.Count;
        if (count == -1)
            count = total;

        logger.LogInformation("【分组下共有】{count}人", total);
        logger.LogInformation("【目标取关】{count}人" + Environment.NewLine, count);

        //计算共几页
        int totalPage = (int)Math.Ceiling(total / (double)20);

        //从最后一页开始获取
        var req = new GetSpecialFollowingsRequest(long.Parse(ck.UserId), tagId.Value)
        {
            Pn = totalPage,
        };
        List<UpInfo> followings = (await relationApi.GetFollowingsByTag(req, ck.ToString())).Data;
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
                if (pn <= 0)
                    break;
                req.Pn = pn;
                followings = (await relationApi.GetFollowingsByTag(req, ck.ToString())).Data;
                followings.Reverse();
            }
        }

        logger.LogInformation("开始取关..." + Environment.NewLine);
        int success = 0;
        for (int i = 1; i <= targetList.Count && i <= count; i++)
        {
            UpInfo info = targetList[i - 1];

            logger.LogInformation("【序号】{num}", i);
            logger.LogInformation("【UP】{up}", info.Uname);

            if (_unfollowBatchedTaskOptions.RetainUidList.Contains(info.Mid.ToString()))
            {
                logger.LogInformation("【取关结果】白名单，跳过" + Environment.NewLine);
                continue;
            }

            string modifyReferer = string.Format(
                RelationApiConstant.ModifyReferer,
                ck.UserId,
                tagId
            );
            var modifyReq = new ModifyRelationRequest(info.Mid, ck.BiliJct);
            var re = await relationApi.ModifyRelation(modifyReq, ck.ToString(), modifyReferer);

            if (re.Code == 0)
            {
                logger.LogInformation("【取关结果】成功" + Environment.NewLine);
                success++;
            }
            else
            {
                logger.LogInformation("【取关结果】失败");
                logger.LogInformation("【原因】{msg}" + Environment.NewLine, re.Message);
            }
        }

        logger.LogInformation("【本次共取关】{count}人", success);

        //计算剩余
        tag = await GetTag(_unfollowBatchedTaskOptions.GroupName, ck);
        logger.LogInformation("【分组下剩余】{count}人", tag?.Count ?? 0);
    }

    /// <summary>
    /// 获取分组（标签）
    /// </summary>
    /// <param name="groupName"></param>
    /// <param name="ck"></param>
    /// <returns></returns>
    private async Task<TagDto> GetTag(string groupName, BiliCookie ck)
    {
        string getTagsReferer = string.Format(RelationApiConstant.GetTagsReferer, ck.UserId);
        List<TagDto> tagList = (await relationApi.GetTags(ck.ToString(), getTagsReferer)).Data;
        TagDto tag = tagList.FirstOrDefault(x => x.Name == groupName);
        return tag;
    }

    /// <summary>
    /// 计算升级时间
    /// </summary>
    /// <param name="useInfo"></param>
    /// <returns>升级时间</returns>
    public int CalculateUpgradeTime(UserInfo useInfo)
    {
        double availableCoins =
            decimal.ToDouble(useInfo.Money ?? 0) - _dailyTaskOptions.NumberOfProtectedCoins;
        long needExp = useInfo.Level_info.GetNext_expLong() - useInfo.Level_info.Current_exp;
        int needDay;

        if (availableCoins < 0)
            needDay = (int)(
                (double)needExp / 25
                + _dailyTaskOptions.NumberOfProtectedCoins
                - Math.Abs(availableCoins)
            );

        switch (_dailyTaskOptions.NumberOfCoins)
        {
            case 0:
                needDay = (int)(needExp / 15);
                break;
            case 1:
                needDay = (int)(needExp / 25);
                break;
            default:
                int dailyExpAvailable = 15 + _dailyTaskOptions.NumberOfCoins * 10;
                double needFrontDay = availableCoins / (_dailyTaskOptions.NumberOfCoins - 1);

                if ((double)needExp / dailyExpAvailable > needFrontDay)
                    needDay = (int)(
                        needFrontDay + (needExp - dailyExpAvailable * needFrontDay) / 25
                    );
                else
                    needDay = (int)(needExp / dailyExpAvailable);
                break;
        }

        return needDay;
    }
}
