using System.Collections.Generic;
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
    /// <remarks>
    /// 修复：重复签到返回非 0 code 或抛 400 异常，不应误报"失败"
    /// （B 站 ClockIn 对已签到账号返回 invalid_argument，msg 字段与 DTO 的
    /// Message 长度不同无法反序列化，故原日志显示"原因 null"）
    /// </remarks>
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
            logger.LogInformation("【签到结果】已签到过（重复签到触发异常，忽略）");
            return;
        }

        if (response.Code == 0)
        {
            logger.LogInformation("【签到结果】成功");
        }
        else
        {
            logger.LogInformation("【签到结果】已签到过或失败（code={Code}）", response.Code);
        }
    }

    /// <summary>
    /// 漫画分享（每日 +5 积分，SeasonV2 per_task.push_point）。
    /// 实测仅需网页 Cookie，返回 data.point 为本次获得的积分；重复分享返回 msg"今日已分享"。
    /// </summary>
    public async Task MangaShare(BiliCookie ck)
    {
        try
        {
            var response = await mangaApi.ShareComic(
                _dailyTaskOptions.DevicePlatform,
                ck.ToString()
            );
            if (response.Code == 0)
            {
                int point = response.Data?.Point ?? 0;
                logger.LogInformation(
                    "【分享结果】成功{PointSuffix}",
                    point > 0 ? $"（+{point}积分）" : ""
                );
            }
            else
            {
                logger.LogInformation(
                    "【分享结果】已分享过或失败（code={Code} msg={Msg}）",
                    response.Code,
                    response.Message ?? "(null)"
                );
            }
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "【分享结果】异常");
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
    ///   3. 两者都未配置且 <see cref="MangaTaskOptions.UseSeasonBookList"/> 开启时，自动调
    ///      user.v1.SeasonV2/GetSeasonInfo 获取"今日推荐（0点更新）"书单
    ///      （data.day_task.book_task，B 站官方每日阅读任务书单，仅需网页 Cookie 即可），
    ///      取前 <see cref="MangaTaskOptions.MangaReadCount"/> 本循环 ReadManga；
    ///   4. 以上都没有 → 打"跳过"日志并 return。
    ///
    /// 注意：AddHistory 仅上报阅读记录（实测 ep_id 不校验，任意值即可）；
    /// B 站奖励仍要求用户累计阅读时长（read_min 分钟/本），这部分依赖 App 端计时上报，
    /// 工具尽力上报 CompleteReadTask（部分版本路径 404，属已知限制）。
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
        else if (_mangaTaskOptions.UseSeasonBookList)
        {
            var fetched = await FetchSeasonBookList(ck);
            int target =
                _mangaTaskOptions.MangaReadCount > 0
                    ? _mangaTaskOptions.MangaReadCount
                    : fetched.Count;
            // 遍历书单，直到凑够 target 本
            foreach (var item in fetched)
            {
                if (list.Count >= target)
                    break;
                // 实测 bookshelf.v1.Bookshelf/AddHistory 不校验 ep_id，
                // 书单只下发 comic_id（ep_id 需 App 签名接口才能查），这里用 1 占位即可上报
                list.Add((item.ComicId, 1));
                logger.LogInformation(
                    "【漫画阅读】今日书单 {Idx}：comic_id={ComicId} {Title}（需读满 {ReadMin} 分钟）",
                    list.Count,
                    item.ComicId,
                    item.Title ?? "(null)",
                    item.ReadMin
                );
            }
        }

        // 2) 都没有 → 跳过（不再像旧逻辑一样早退后报"成功"骗用户）
        if (list.Count == 0)
        {
            logger.LogInformation(
                "【漫画阅读】跳过：未配置 CustomComicId / CustomComics，且未启用 SeasonBookList（详见 issue #1098）"
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
    /// 获取"今日推荐（0点更新）"每日阅读书单
    /// （user.v1.SeasonV2/GetSeasonInfo，B 站官方每日阅读任务书单，仅需网页 Cookie）
    /// </summary>
    private async Task<List<BookTaskItem>> FetchSeasonBookList(BiliCookie ck)
    {
        var result = new List<BookTaskItem>();
        try
        {
            var resp = await mangaApi.GetSeasonInfo(new SeasonRequest { Type = 1 }, ck.ToString());
            if (resp.Code != 0 || resp.Data?.DayTask?.BookTask == null)
            {
                logger.LogInformation(
                    "【漫画阅读】获取今日书单失败：code={Code} msg={Msg}",
                    resp.Code,
                    resp.Message ?? "(null)"
                );
                return result;
            }

            result = resp.Data.DayTask.BookTask;
            logger.LogInformation("【漫画阅读】获取到今日书单 {Count} 本", result.Count);
            if (resp.Data.DayTask.RewardProgress > 0)
            {
                logger.LogInformation(
                    "【漫画阅读】今日已读完 {Progress} 本（奖励进度）",
                    resp.Data.DayTask.RewardProgress
                );
            }
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "【漫画阅读】获取今日书单异常");
        }

        return result;
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
