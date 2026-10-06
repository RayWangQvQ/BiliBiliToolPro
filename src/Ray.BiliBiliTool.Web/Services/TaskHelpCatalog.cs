namespace Ray.BiliBiliTool.Web.Services;

public sealed record TaskHelpItem(string Name, string Description, string? SettingKey = null);

public sealed record TaskHelp(
    string Key,
    string ConfigurationPage,
    string Introduction,
    string Timing,
    IReadOnlyList<TaskHelpItem> Features
);

public static class TaskHelpCatalog
{
    public static IReadOnlyList<TaskHelp> All { get; } =
    [
        new(
            "DailyTaskAppService",
            "DailyJobConfig",
            "完成账号登录、视频观看、分享和投币等日常操作，用于积累主站等级经验。投币会消耗账号硬币，执行时会扣除当天已完成的投币量。",
            "适合每天执行一次。观看、分享和投币可以分别设置。",
            [
                new("登录", "读取账号状态，完成每日登录检查。"),
                new(
                    "是否观看视频",
                    "选择视频并提交观看记录，已完成当天观看项时跳过。",
                    "IsWatchVideo"
                ),
                new(
                    "是否分享视频",
                    "提交所选视频的分享记录，已完成当天分享项时跳过。",
                    "IsShareVideo"
                ),
                new(
                    "每日投币数 [0-5]",
                    "设定当天总投币目标，范围为 0～5 枚。设置为 0 时跳过投币。",
                    "NumberOfCoins"
                ),
                new(
                    "要保留的硬币数量",
                    "只使用超过保留数量的硬币进行投币。",
                    "NumberOfProtectedCoins"
                ),
                new(
                    "是否开启专栏投币",
                    "优先给专栏文章投币，专栏未完成目标时继续尝试视频投币。",
                    "IsDonateCoinForArticle"
                ),
                new(
                    "达到指定等级后停止投币",
                    "按每个 B 站账号的等级判断，达到所选等级后停止视频和专栏投币，其他已开启的日常操作继续执行。",
                    "CoinDonationStopLevel"
                ),
                new(
                    "后台随机延迟上限（分钟）",
                    "定时每日任务执行前随机等待。留空使用全局设置，填写 0 可立即执行，手动执行和补做立即开始。",
                    "RandomDelayMaxMinutes"
                ),
                new("投币时点赞", "给视频投币时同时点赞。", "SelectLike"),
                new(
                    "支持的 UP 主 UID",
                    "优先从这些 UP 主的内容中选择观看、分享和投币目标。填写 UID，多个用英文逗号分隔。",
                    "SupportUpIds"
                ),
                new(
                    "执行客户端操作时的平台",
                    "选择客户端操作使用的平台。漫画签到和阅读也会使用此设置。",
                    "DevicePlatform"
                ),
                new(
                    "大会员福利",
                    "每日任务结束时会尝试领取会员福利，是否领取由「大会员福利」任务开关控制。"
                ),
            ]
        ),
        new(
            "MangaTaskAppService",
            "MangaTaskConfig",
            "在哔哩哔哩漫画完成签到，并为指定漫画章节提交阅读记录。漫画任务与主站视频任务分别执行。",
            "适合每天执行一次。漫画编号设为 0 时只签到。",
            [
                new("漫画签到", "执行漫画平台的每日签到。签到结果可在漫画应用查看。"),
                new("漫画编号", "漫画地址中 mc 后面的数字，设为 0 时只签到。", "CustomComicId"),
                new(
                    "章节编号",
                    "指定该漫画的一个章节，填写与漫画作品对应的章节编号。",
                    "CustomEpId"
                ),
                new("阅读记录", "提交指定章节的阅读记录，作品与章节编号需要匹配。"),
            ]
        ),
        new(
            "MangaPrivilegeTaskAppService",
            "MangaPrivilegeTaskConfig",
            "为有大会员资格的账号领取哔哩哔哩漫画的漫读券权益，用于阅读适用的付费漫画章节。",
            "可以每天检查一次，领取周期和券的使用范围以漫画权益页为准。",
            [
                new("会员资格", "执行前读取大会员状态，有会员资格时尝试领取。"),
                new("领取漫读券", "领取账号当前可用的漫画会员权益，数量由漫画平台返回。"),
                new("查看领取结果", "领取后可在哔哩哔哩漫画的券包查看可用券及有效期。"),
            ]
        ),
        new(
            "Silver2CoinTaskAppService",
            "Silver2CoinTaskConfig",
            "使用直播钱包中的银瓜子兑换主站硬币，先读取钱包余额和当天剩余兑换次数，再提交兑换。",
            "可以每天执行一次，每次运行提交一次兑换请求。",
            [
                new("直播钱包", "查询银瓜子余额、硬币余额和当天剩余兑换次数。"),
                new("兑换硬币", "有剩余兑换次数时提交兑换，兑换数量由直播钱包返回。"),
                new("兑换资格", "余额要求、兑换比例和可用次数由 B 站直播钱包决定。"),
            ]
        ),
        new(
            "ChargeTaskAppService",
            "ChargeTaskConfig",
            "使用年度大会员的 B 币券给 UP 主充电。当前实现会使用全部 B 币券余额，余额达到 2 B 币券时执行，成功后发送充电留言。",
            "适合安排在券的有效期内执行，例如每月最后一天。先在 B 站卡券包确认有效期和充电对象。",
            [
                new(
                    "充电 UP 主 UID",
                    "填写接收充电的 UP 主 UID。未填写或填写 -1 时，使用项目预设的支持作者对象。",
                    "AutoChargeUpId"
                ),
                new(
                    "指定充电对象",
                    "开启后显示充电 UP 主 UID 输入框。点击「支持作者」可选择项目预设对象，保存后生效。",
                    "SpecifyUp"
                ),
                new("B 币券余额", "年度大会员且券余额达到 2 时，按全部券余额提交充电。"),
                new(
                    "充电后留言",
                    "充电成功后发送填写的留言，留空时随机选择一条默认留言。",
                    "ChargeComment"
                ),
            ]
        ),
        new(
            "VipPrivilegeTaskAppService",
            "VipPrivilegeConfig",
            "为年度大会员尝试领取当前可用的 B 币券和会员福利。领取后的卡券可在 B 站大会员卡券包查看。",
            "可以每天检查一次，实际领取周期由账号卡券包决定。",
            [
                new("年度大会员", "执行前读取会员类型，年度大会员进入领取流程。"),
                new("B 币券与福利", "分别尝试领取 B 币券和会员福利，券种和可领取状态由 B 站返回。"),
                new(
                    "与每日任务的关系",
                    "此开关也控制每日任务里的福利领取步骤。独立执行时间用于单独安排领取。"
                ),
            ]
        ),
        new(
            "VipBigPointAppService",
            "VipBigPointConfig",
            "为大会员完成积分中心的签到、福利、体验和部分日常任务，积累大会员积分。积分可在 B 站积分中心兑换当前提供的权益。",
            "适合每天执行一次，任务清单由积分中心下发。",
            [
                new("签到与领取任务", "签到后读取积分中心，领取尚未领取的可执行任务。"),
                new("福利与体验", "提交福利任务和体验任务的完成记录。"),
                new(
                    "浏览与观看",
                    "处理浏览装扮商城、会员购、追番和影视，以及积分中心提供的观看任务。"
                ),
                new("购买类任务", "购买会员购商品、影片和装扮的任务需要自行在 B 站完成。"),
                new(
                    "番剧编号",
                    "这是旧版观看配置。当前观看任务使用积分中心下发的任务流程，此项保留原值。"
                ),
            ]
        ),
        new(
            "LiveLotteryTaskAppService",
            "LiveLotteryTaskConfig",
            "查找直播间的「天选时刻」抽奖，根据奖品关键词和主播名单筛选后报名。部分抽奖会新增主播关注，这些关注可自动放入「天选时刻」分组。",
            "按所选时间搜索当时正在进行的抽奖，报名后可在 B 站查看中奖通知。",
            [
                new(
                    "参与范围",
                    "参与无额外条件或需要关注主播的免费抽奖，需要赠送付费礼物或其他条件的活动会跳过。"
                ),
                new(
                    "包含的奖品名称关键字",
                    "奖品名称匹配任意一个关键词即可入选，多个用 | 分隔。留空时不限定奖品名称。",
                    "IncludeAwardNames"
                ),
                new(
                    "排除的奖品名称关键字",
                    "奖品名称包含任意排除关键词时跳过，排除条件优先于包含条件。",
                    "ExcludeAwardNames"
                ),
                new(
                    "主播 UID 黑名单",
                    "不参加这些主播的抽奖。填写 UID，多个用英文逗号分隔。",
                    "DenyUids"
                ),
                new(
                    "抽奖后自动分组",
                    "将本次抽奖新增的关注整理到「天选时刻」分组，方便后续管理。",
                    "AutoGroupFollowings"
                ),
            ]
        ),
        new(
            "LiveFansMedalAppService",
            "LiveFansMedalTaskConfig",
            "为已有直播粉丝牌完成点赞、弹幕和观看任务，用于点亮粉丝牌并积累亲密度。卡片展示每个粉丝牌的等级和 B 站当天返回的任务进度。",
            "默认自动检查并处理剩余任务。开播后点赞，观看通过心跳累计时长，离线弹幕在主播下播后执行。每日次数和时长可自行调整。",
            [
                new(
                    "按主播状态自动执行",
                    "后台按检查间隔读取任务进度和主播状态。点赞等待开播，观看无需等待开播，离线弹幕等待下播。完成后停止对应互动。",
                    "UseLiveStateMonitoring"
                ),
                new(
                    "检查间隔",
                    "设置每次检查主播状态之间的分钟数，默认每 5 分钟检查一次。",
                    "MonitorIntervalMinutes"
                ),
                new(
                    "仅为白名单主播执行任务",
                    "开启后只执行卡片上选中的主播。白名单为空时不执行，排除设置优先生效。",
                    "OnlySelectedAnchors"
                ),
                new(
                    "排除此主播",
                    "在粉丝牌卡片勾选要排除的主播，确认后立即保存并停止该主播的自动互动。取消排除同样在确认后立即保存。所有账号共用排除名单。",
                    "ExcludedAnchorIds"
                ),
                new(
                    "置顶主播",
                    "点击卡片上的置顶按钮后保存配置，优先展示常用主播。所有账号共用置顶名单，已排除主播仍排在列表末尾。",
                    "PinnedAnchorIds"
                ),
                new(
                    "跟随 B 站每日任务量",
                    "开启后跟随每个粉丝牌当天剩余的任务量，达到目标后结束。",
                    "FollowDailyTaskLimit"
                ),
                new(
                    "每个粉丝牌的点赞次数",
                    "自动模式默认每个粉丝牌每日点赞 300 次，可自行修改。当天已有进度计入额度，主播开播时执行。定时自定义模式按每次次数执行。",
                    "LikeNumber"
                ),
                new(
                    "每个粉丝牌的弹幕次数",
                    "自动模式默认每个粉丝牌每日发送 10 条，可自行修改。当天已有进度计入额度，弹幕逐条发送。定时自定义模式按每次次数执行。",
                    "SendDanmakuNumber"
                ),
                new(
                    "每个粉丝牌的观看时长（分钟）",
                    "自动模式默认每个粉丝牌每日观看 150 分钟，可自行修改。当天已有进度计入时长，主播下播后继续累计。定时自定义模式按每次时长执行。",
                    "HeartBeatNumber"
                ),
                new("弹幕内容", "填写发送到直播间的弹幕文字。", "DanmakuContent"),
                new(
                    "仅在主播未开播时发弹幕",
                    "开启后只在未开播的直播间发送弹幕。",
                    "DanmakuOnlyWhenOffline"
                ),
                new(
                    "点赞",
                    "在主播直播时执行，用于完成点赞任务，也可点亮符合条件的粉丝牌。",
                    "EnableLike"
                ),
                new(
                    "弹幕",
                    "发送配置的弹幕内容。开启「仅在主播未开播时」后，在未开播的房间执行。",
                    "EnableDanmaku"
                ),
                new(
                    "观看直播",
                    "同一账号每次观看一个直播间，其他主播依次排队。已点亮粉丝牌的观看任务无需等待开播，完成情况以 B 站任务进度为准。",
                    "EnableWatch"
                ),
                new(
                    "亲密度储蓄",
                    "储蓄已满的已点亮粉丝牌会暂停升级任务，待点亮的粉丝牌仍可执行点亮任务。"
                ),
                new(
                    "观看连续失败次数上限",
                    "在「异常处理」中设置。达到上限时结束当前粉丝牌的观看任务。",
                    "HeartBeatSendGiveUpThreshold"
                ),
                new(
                    "弹幕连续失败次数上限",
                    "在「异常处理」中设置。达到上限时结束当前粉丝牌的弹幕任务。",
                    "SendDanmakugiveUpThreshold"
                ),
            ]
        ),
        new(
            "UnfollowBatchedTaskAppService",
            "UnfollowBatchedTaskConfig",
            "从指定关注分组中批量取消关注，常用于整理直播抽奖新增的主播关注。执行后账号关注列表会发生变化。",
            "适合按需执行。保存前确认分组名称、本次处理数量和保留名单。",
            [
                new(
                    "取关的分组名称",
                    "只处理与填写名称匹配的关注分组，默认分组为「天选时刻」。",
                    "GroupName"
                ),
                new(
                    "本次取关个数",
                    "从分组列表末尾向前选择最多指定数量的账号。设置为 0 时不取关。",
                    "Count"
                ),
                new(
                    "保留的 UP 主 UID",
                    "选中的账号在保留名单中时跳过，不会补选其他账号填满数量。填写 UID，多个用英文逗号分隔。",
                    "RetainUids"
                ),
            ]
        ),
        new(
            "Notification",
            "NotificationConfig",
            "管理活动前的登录状态检查、Cookie 过期提醒和每日自动任务汇总，通知通过 Server 酱发送。",
            "过期提醒同一账号每天最多一条，任务汇总每天最多一条。",
            [
                new(
                    "自动检查登录状态",
                    "每天首次活动前检查登录状态，过期账号暂停活动，重新登录后立即复检。",
                    "AutomaticCheckEnabled"
                ),
                new("手动检查", "在账号管理点击「检测」查看登录状态。"),
                new("Cookie 过期提醒", "检查发现账号过期时发送提醒，方便重新登录。", "Enabled"),
                new("每日任务汇总提醒", "开启后发送所选自动任务的当天执行结果。", "FailureEnabled"),
                new(
                    "最终汇总时间",
                    "当天自动任务全部结束后发送一次。到所选时间仍未完成时，汇总失败和未完成项。默认每天 23:55，采用 UTC+8。",
                    "DailySummaryTime"
                ),
                new(
                    "选择需要纳入汇总的任务",
                    "选择汇总中展示的任务。手动执行和补做不单独触发推送。",
                    "TaskKeys"
                ),
                new(
                    "SendKey",
                    "填写 Server 酱 SendKey 并保存。保存后显示已配置状态和掩码，两个提醒共用该配置。",
                    "SendKey"
                ),
            ]
        ),
        new(
            "AutoRecover",
            "",
            "定期检查当天到期却漏做或失败的任务，并重新尝试执行。自动补做和手动补做都记录执行结果。",
            "检查间隔可设置为 1～24 小时，每项每天最多自动补做 3 次。",
            [
                new("自动补做", "开启后按检查间隔寻找可补做项，分享任务由用户手动处理。", "Enable"),
                new("每几小时检查一次", "决定每隔几小时检查一次当天任务。", "IntervalHours"),
                new(
                    "记录保留天数",
                    "控制执行记录保留时间，范围为 1～90 天。",
                    "RecordRetentionDays"
                ),
            ]
        ),
    ];

    public static TaskHelp? Find(string key) => All.FirstOrDefault(help => help.Key == key);

    public static string? SettingDescription(string taskKey, string settingKey) =>
        Find(taskKey)?.Features.FirstOrDefault(item => item.SettingKey == settingKey)?.Description;

    public static string? Introduction(string taskKey, string? itemKey = null) =>
        (taskKey, itemKey) switch
        {
            ("DailyTaskAppService", "Login") => "读取账号登录状态，完成每日登录检查。",
            ("DailyTaskAppService", "Watch") => "提交视频观看记录，用于完成当天的观看任务。",
            ("DailyTaskAppService", "Share") => "提交视频分享记录，用于完成当天的分享任务。",
            ("DailyTaskAppService", "DonateCoin") =>
                "使用账号硬币完成当天投币目标，遵循保留硬币和停止投币等级设置。",
            ("DailyTaskAppService", "VipPrivilege") => Find(
                "VipPrivilegeTaskAppService"
            )!.Introduction,
            _ => Find(taskKey)?.Introduction,
        };

    public static string? ConfigurationUrl(string taskKey, string? itemKey = null)
    {
        var help = Find(
            taskKey == "DailyTaskAppService" && itemKey == "VipPrivilege"
                ? "VipPrivilegeTaskAppService"
                : taskKey
        );
        return string.IsNullOrEmpty(help?.ConfigurationPage)
            ? null
            : $"/Configurations/{help.ConfigurationPage}";
    }
}
