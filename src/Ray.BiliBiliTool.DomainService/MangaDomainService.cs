using System.Collections.Generic;
using System.Text.RegularExpressions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Ray.BiliBiliTool.Agent;
using Ray.BiliBiliTool.Agent.BiliBiliAgent.Dtos;
using Ray.BiliBiliTool.Agent.BiliBiliAgent.Dtos.MangaApi;
using Ray.BiliBiliTool.Agent.BiliBiliAgent.Dtos.NavApi;
using Ray.BiliBiliTool.Agent.BiliBiliAgent.Interfaces;
using Ray.BiliBiliTool.Config.Options;
using Ray.BiliBiliTool.DomainService.Interfaces;

namespace Ray.BiliBiliTool.DomainService;

/// <summary>
/// 漫画
/// </summary>
public class MangaDomainService(
    ILogger<MangaDomainService> logger,
    IMangaApi mangaApi,
    IOptionsMonitor<MangaTaskOptions> mangaTaskOptions,
    IOptionsMonitor<DailyTaskOptions> dailyTaskOptions,
    IOptionsMonitor<VipPrivilegeOptions> vipPrivilegeOptions
) : IMangaDomainService
{
    private readonly MangaTaskOptions _mangaTaskOptions = mangaTaskOptions.CurrentValue;
    private readonly DailyTaskOptions _dailyTaskOptions = dailyTaskOptions.CurrentValue;
    private readonly VipPrivilegeOptions _vipPrivilegeOptions = vipPrivilegeOptions.CurrentValue;

    /// <summary>
    /// 漫画签到
    /// </summary>
    public async Task MangaSign(BiliCookie ck)
    {
        BiliApiResponse response;
        try
        {
            response = await mangaApi.ClockIn(_dailyTaskOptions.DevicePlatform, ck.ToString());
        }
        catch (Exception)
        {
            //ignore
            //重复签到会报400异常,这里忽略掉
            logger.LogInformation("【签到结果】失败");
            logger.LogInformation("【原因】今日已签到过，无法重复签到");
            return;
        }

        if (response.Code == 0)
        {
            logger.LogInformation("【签到结果】成功");
        }
        else
        {
            logger.LogInformation("【签到结果】失败");
            logger.LogInformation("【原因】{msg}", response.Message);
        }
    }

    /// <summary>
    /// 漫画阅读
    /// </summary>
    /// <remarks>
    /// 行为说明（修复 issue #1098：每日阅读必须读 B 站当天指定的书，而非任意 5 本）：
    ///   1. 优先读取 <see cref="MangaTaskOptions.CustomComics"/> 多本列表（每本独立调用 ReadManga）；
    ///   2. 若 <see cref="MangaTaskOptions.CustomComics"/> 为空，回退使用
    ///      <see cref="MangaTaskOptions.CustomComicId"/> + <see cref="MangaTaskOptions.CustomEpId"/> 单本配置
    ///      （向后兼容 PR #562 的旧配置）；
    ///   3. 两者都未配置且 <see cref="MangaTaskOptions.UseHomeRecommend"/> 开启时，自动抓取
    ///      "漫画首页推荐"（comic.v1.Comic/HomeRecommend，B 站每日指定的推荐漫画），取前
    ///      <see cref="MangaTaskOptions.MangaReadCount"/> 本，解析每本的 (comic_id, ep_id) 后循环 ReadManga；
    ///   4. 以上都没有 → 打"跳过"日志并 return。
    ///
    /// 注意：AddHistory 仅上报阅读记录，B 站奖励仍要求用户在 App 端真实停留 5 分钟/本，
    /// 这部分风控不在工具能力范围。
    /// </remarks>
    public async Task MangaRead(BiliCookie ck)
    {
        // 1) 收集要读的漫画 (comic_id, ep_id)
        var list = new List<(long ComicId, long EpId)>();

        if (_mangaTaskOptions.CustomComics != null && _mangaTaskOptions.CustomComics.Count > 0)
        {
            foreach (var c in _mangaTaskOptions.CustomComics)
            {
                if (c != null && c.ComicId > 0)
                    list.Add((c.ComicId, c.EpId));
            }
        }
        else if (_mangaTaskOptions.CustomComicId > 0)
        {
            list.Add((_mangaTaskOptions.CustomComicId, _mangaTaskOptions.CustomEpId));
        }
        else if (_mangaTaskOptions.UseHomeRecommend)
        {
            var fetched = await FetchHomeRecommendList(ck);
            int take =
                _mangaTaskOptions.MangaReadCount > 0
                    ? _mangaTaskOptions.MangaReadCount
                    : fetched.Count;
            foreach (var item in fetched.Take(take))
            {
                long epId = ParseEpId(item);
                if (item.ComicId > 0 && epId > 0)
                {
                    list.Add((item.ComicId, epId));
                }
                else
                {
                    logger.LogInformation(
                        "【漫画阅读】跳过推荐项（无法解析 ep_id）：comic_id={ComicId} title={Title}",
                        item.ComicId,
                        item.Title ?? "(null)"
                    );
                }
            }
        }

        // 2) 都没有 → 跳过（不再像旧逻辑一样早退后报"成功"骗用户）
        if (list.Count == 0)
        {
            logger.LogInformation(
                "【漫画阅读】跳过：未配置 CustomComicId / CustomComics，且未启用 HomeRecommend（详见 issue #1098）"
            );
            return;
        }

        logger.LogInformation(
            "【漫画阅读】开始：本次将读 {Count} 本（注意 B 站奖励需配合 App 端停留 5 分钟/本）",
            list.Count
        );

        // 3) 循环调用 ReadManga
        int successCount = 0;
        for (int idx = 0; idx < list.Count; idx++)
        {
            var (comicId, epId) = list[idx];
            int displayIdx = idx + 1;
            try
            {
                BiliApiResponse response = await mangaApi.ReadManga(
                    _dailyTaskOptions.DevicePlatform,
                    comicId,
                    epId,
                    ck.ToString()
                );

                if (response.Code == 0)
                {
                    successCount++;
                    logger.LogInformation(
                        "【漫画阅读】{Idx}/{Total} comic_id={ComicId} ep_id={EpId} 成功",
                        displayIdx,
                        list.Count,
                        comicId,
                        epId
                    );
                }
                else
                {
                    logger.LogInformation(
                        "【漫画阅读】{Idx}/{Total} comic_id={ComicId} ep_id={EpId} 失败",
                        displayIdx,
                        list.Count,
                        comicId,
                        epId
                    );
                    logger.LogInformation("【原因】{msg}", response.Message);
                }
            }
            catch (Exception ex)
            {
                logger.LogError(
                    ex,
                    "【漫画阅读】{Idx}/{Total} comic_id={ComicId} 异常",
                    displayIdx,
                    list.Count,
                    comicId
                );
            }
        }

        logger.LogInformation(
            "【漫画阅读】结束：共 {Total} 本，成功 {Success} 本",
            list.Count,
            successCount
        );
    }

    /// <summary>
    /// 抓取漫画首页推荐，返回当日指定推荐漫画列表
    /// </summary>
    private async Task<List<ComicRecommendItem>> FetchHomeRecommendList(BiliCookie ck)
    {
        var result = new List<ComicRecommendItem>();
        try
        {
            var resp = await mangaApi.HomeRecommend(
                new HomeRecommendRequest { PageNum = 1 },
                ck.ToString()
            );
            if (resp.Code != 0 || resp.Data?.List == null)
            {
                logger.LogInformation(
                    "【漫画阅读】获取首页推荐失败：code={Code} msg={Msg}",
                    resp.Code,
                    resp.Message ?? "(null)"
                );
                return result;
            }

            result = resp.Data.List;
            logger.LogInformation("【漫画阅读】获取到首页推荐 {Count} 本", result.Count);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "【漫画阅读】获取首页推荐异常");
        }

        return result;
    }

    /// <summary>
    /// 从推荐项解析 ep_id：优先 jump_value 中的 cid=，回退 pv_info.cid（字符串）
    /// </summary>
    private long ParseEpId(ComicRecommendItem item)
    {
        if (!string.IsNullOrEmpty(item.JumpValue))
        {
            var m = Regex.Match(item.JumpValue, @"cid=(\d+)");
            if (m.Success && long.TryParse(m.Groups[1].Value, out var cid) && cid > 0)
                return cid;
        }

        if (
            item.PvInfo != null
            && !string.IsNullOrEmpty(item.PvInfo.Cid)
            && long.TryParse(item.PvInfo.Cid, out var cid2)
            && cid2 > 0
        )
            return cid2;

        return 0;
    }

    /// <summary>
    /// 获取大会员漫画权益
    /// </summary>
    /// <param name="reason_id">权益号，由https://api.bilibili.com/x/vip/privilege/my得到权益号数组，取值范围为数组中的整数
    /// 这里为方便直接取1，为领取漫读劵，暂时不取其他的值</param>
    public async Task ReceiveMangaVipReward(int reason_id, UserInfo userInfo, BiliCookie ck)
    {
        if (userInfo.GetVipType() == 0)
        {
            logger.LogInformation("不是会员，跳过");
            return;
        }

        int day = DateTime.Today.Day;
        logger.LogInformation("【今天】{day}号", day);

        var response = await mangaApi.ReceiveMangaVipReward(reason_id, ck.ToString());
        if (response.Code == 0)
        {
            logger.LogInformation("【领取结果】成功");
            logger.LogInformation($"【获取】{response.Data.Amount}张漫读劵");
        }
        else
        {
            logger.LogInformation("【领取结果】失败");
            logger.LogInformation("【原因】{msg}", response.Message);
        }
    }
}
