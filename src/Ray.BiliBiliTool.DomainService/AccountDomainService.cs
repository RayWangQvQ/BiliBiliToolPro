using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Ray.BiliBiliTool.Agent;
using Ray.BiliBiliTool.Agent.BiliBiliAgent.Dtos;
using Ray.BiliBiliTool.Agent.BiliBiliAgent.Dtos.ApiApi.Daily;
using Ray.BiliBiliTool.Agent.BiliBiliAgent.Dtos.ApiApi.Relation;
using Ray.BiliBiliTool.Agent.BiliBiliAgent.Dtos.ApiApi.UpInfo;
using Ray.BiliBiliTool.Agent.BiliBiliAgent.Dtos.NavApi;
using Ray.BiliBiliTool.Agent.BiliBiliAgent.Interfaces;
using Ray.BiliBiliTool.Config.Options;
using Ray.BiliBiliTool.Domain.Exceptions;
using Ray.BiliBiliTool.DomainService.Interfaces;
using UpInfoDto = Ray.BiliBiliTool.Agent.BiliBiliAgent.Dtos.ApiApi.UpInfo.UpInfo;

namespace Ray.BiliBiliTool.DomainService;

/// <summary>
/// 账户
/// </summary>
public class AccountDomainService(
    ILogger<AccountDomainService> logger,
    IApiApi apiApi,
    INavApi navApi,
    IOptionsMonitor<UnfollowBatchedTaskOptions> unfollowBatchedTaskOptions,
    IOptionsMonitor<DailyTaskOptions> dailyTaskOptions
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
        BiliApiResponse<UserInfo> apiResponse = await navApi.GetNavAsync(cookie.ToString());

        if (apiResponse.Code != 0 || !apiResponse.Data!.IsLogin)
        {
            throw new BiliBusinessException("登录失败，请检查Cookie");
        }

        UserInfo useInfo = apiResponse.Data;

        logger.LogInformation("【用户名】{0}", useInfo.GetFuzzyUname());
        logger.LogInformation("【会员类型】{0}", useInfo.VipType.Description());
        logger.LogInformation("【会员状态】{0}", useInfo.VipStatus.Description());
        logger.LogInformation("【硬币余额】{0}", useInfo.Money ?? 0);

        if (useInfo.Level_info?.Current_level < 6)
        {
            logger.LogInformation(
                "【距升级Lv{0}】预计{1}天",
                useInfo.Level_info.Current_level + 1,
                CalculateUpgradeTime(useInfo)
            );
        }
        else
        {
            logger.LogInformation("【当前经验】{0}", useInfo.Level_info?.Current_exp);
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
        BiliApiResponse<DailyTaskInfo> apiResponse = await apiApi.GetDailyTaskRewardInfoAsync(
            ck.ToString()
        );
        if (apiResponse.Code == 0)
        {
            logger.LogDebug("请求本日任务完成状态成功");
            result = apiResponse.Data ?? result;
        }
        else
        {
            logger.LogWarning("获取今日任务完成状态失败：{result}", apiResponse.ToJsonStr());
            result = (await apiApi.GetDailyTaskRewardInfoAsync(ck.ToString())).Data ?? result;
            //todo:偶发性请求失败，再请求一次，这么写很丑陋，待用polly再框架层面实现
        }

        return result!;
    }

    /// <summary>
    /// 取关
    /// </summary>
    /// <param name="groupName"></param>
    /// <param name="count"></param>
    public async Task UnfollowBatched(BiliCookie ck)
    {
        int count = _unfollowBatchedTaskOptions.Count;
        if (count < -1)
            throw new BiliBusinessException("批量取关执行数量应为非负数或 -1");
        if (count == 0)
        {
            logger.LogInformation("执行数量为 0，跳过取关");
            return;
        }
        logger.LogInformation("【分组名】{group}", _unfollowBatchedTaskOptions.GroupName);

        //根据分组名称获取tag
        TagDto? tag = await GetTag(_unfollowBatchedTaskOptions.GroupName, ck);
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
        if (total < 0)
            throw new BiliBusinessException("获取关注分组返回了无效的关注数量");
        if (count == -1)
            count = total;

        logger.LogInformation("【分组下共有】{count}人", total);
        logger.LogInformation("【目标取关】{count}人" + Environment.NewLine, count);

        //计算共几页
        int totalPage = (int)Math.Ceiling(total / (double)20);

        //从最后一页开始获取
        var targetList = new List<UpInfoDto>();
        var selectedIds = new HashSet<long>();
        for (int page = totalPage; page > 0 && targetList.Count < count; page--)
        {
            var response = await apiApi.GetFollowingsByTag(
                new GetSpecialFollowingsRequest(long.Parse(ck.UserId), tagId.Value) { Pn = page },
                ck.ToString()
            );
            if (response.Code != 0)
                throw new BiliBusinessException(
                    $"获取分组关注第 {page} 页失败：{response.Message}（错误码 {response.Code}）"
                );
            if (response.Data is null || response.Data.Any(item => item is null || item.Mid <= 0))
                throw new BiliBusinessException($"获取分组关注第 {page} 页未返回有效的关注数据");
            foreach (var following in response.Data.AsEnumerable().Reverse())
            {
                if (selectedIds.Add(following.Mid))
                    targetList.Add(following);
                if (targetList.Count >= count)
                    break;
            }
        }

        logger.LogInformation("开始取关..." + Environment.NewLine);
        int success = 0;
        var failures = new List<int>();
        for (int i = 1; i <= targetList.Count && i <= count; i++)
        {
            UpInfoDto info = targetList[i - 1];

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
            var re = await apiApi.ModifyRelation(modifyReq, ck.ToString(), modifyReferer);

            if (re.Code == 0)
            {
                logger.LogInformation("【取关结果】成功" + Environment.NewLine);
                success++;
            }
            else
            {
                failures.Add(re.Code);
                logger.LogInformation("【取关结果】失败");
                logger.LogInformation(
                    "【原因】{msg}（错误码 {code}）" + Environment.NewLine,
                    re.Message,
                    re.Code
                );
            }
        }

        logger.LogInformation("【本次共取关】{count}人", success);
        if (failures.Count > 0)
            throw new BiliBusinessException(
                $"批量取关成功 {success} 人，失败 {failures.Count} 人（错误码 {string.Join("、", failures.Distinct())}）"
            );

        //计算剩余
        try
        {
            tag = await GetTag(_unfollowBatchedTaskOptions.GroupName, ck);
        }
        catch (BiliBusinessException exception)
        {
            throw new BiliBusinessException(
                $"本次已成功取关 {success} 人，剩余数量未确认：{exception.Message}"
            );
        }
        logger.LogInformation("【分组下剩余】{count}人", tag?.Count);
    }

    /// <summary>
    /// 获取分组（标签）
    /// </summary>
    /// <param name="groupName"></param>
    /// <param name="ck"></param>
    /// <returns></returns>
    private async Task<TagDto?> GetTag(string groupName, BiliCookie ck)
    {
        string getTagsReferer = string.Format(RelationApiConstant.GetTagsReferer, ck.UserId);
        var response = await apiApi.GetTags(ck.ToString(), getTagsReferer);
        if (response.Code != 0)
            throw new BiliBusinessException(
                $"获取关注分组失败：{response.Message}（错误码 {response.Code}）"
            );
        if (response.Data is null || response.Data.Any(group => group is null))
            throw new BiliBusinessException("获取关注分组未返回有效的分组数据");
        var tag = response.Data.FirstOrDefault(x => x.Name == groupName);
        return tag;
    }

    /// <summary>
    /// 计算升级时间
    /// </summary>
    /// <param name="useInfo"></param>
    /// <returns>升级时间</returns>
    public int CalculateUpgradeTime(UserInfo useInfo)
    {
        var level = useInfo.Level_info;
        if (level is null || level.Current_level >= 6)
            return 0;

        decimal needExp = (decimal)level.GetNext_expLong() - level.Current_exp;
        if (needExp <= 0)
            return 0;

        int dailyExp =
            5 + (_dailyTaskOptions.IsWatchVideo ? 5 : 0) + (_dailyTaskOptions.IsShareVideo ? 5 : 0);
        int coinLimit = _dailyTaskOptions.ShouldSkipCoinDonation(level.Current_level)
            ? 0
            : Math.Clamp(_dailyTaskOptions.NumberOfCoins, 0, 5);
        decimal availableCoins =
            decimal.Floor(useInfo.Money ?? 0)
            - Math.Max(0, _dailyTaskOptions.NumberOfProtectedCoins);

        // Forecast one login coin per day, spending only above the protected balance.
        decimal ExpAfter(int days) =>
            (decimal)days * dailyExp
            + 10 * Math.Min((decimal)days * coinLimit, Math.Max(0, availableCoins + days));

        int low = 1;
        int high = int.MaxValue;
        while (low < high)
        {
            int middle = low + (high - low) / 2;
            if (ExpAfter(middle) >= needExp)
                high = middle;
            else
                low = middle + 1;
        }
        return low;
    }
}
