# Task help in the Web panel

Checked against the task implementations and Web controls in this repository.

The configuration pages show a short introduction and an expandable reference with separate sections for editable settings and execution behavior. Only items mapped to an actual control have a `SettingKey`. Eligibility checks, automatic steps, purchase tasks handled on Bilibili and inactive legacy fields appear under execution content and conditions. Pages explain when enabling a task reveals its detailed controls. Tasks with no extra active options explicitly offer only the task switch and schedule.

Setting descriptions beside the controls and in the reference come from `TaskHelpCatalog`. The Today page shows a description beneath each task item and links to its configuration page. The catalog includes all ten scheduled activities, notification settings, automatic recovery and the daily subitems.

## Evidence and scope

The deployed application is the source for the actions performed by this build. Public documentation is used for platform terminology and the role of membership benefits. Fixed reward amounts and historical exchange rates are omitted because the current services read eligibility, progress and balances from Bilibili.

| Task | Implementation inspected | Help content |
| --- | --- | --- |
| Daily | `DailyTaskAppService`, `VideoDomainService`, `ArticleDomainService`, `DonateCoinDomainService` | Login, watch/share records, remaining daily coin target, protected coins, article-first donations, Lv.6 setting, supported creators and shared client platform |
| Manga | `MangaTaskAppService`, `MangaDomainService` | Check-in, comic/chapter IDs and check-in-only mode when comic ID is nonpositive |
| Manga privilege | `MangaPrivilegeTaskAppService`, `MangaDomainService.ReceiveMangaVipReward` | Membership check and manga reading coupons |
| Silver to coin | `LiveDomainService.ExchangeSilver2Coin` | Wallet query, remaining exchange allowance and one request per run |
| Charge | `ChargeDomainService`, `ChargeTaskOptions`, `ChargeRequest` | Annual membership, full coupon balance when at least 2, configured or preset recipient and comments |
| VIP privilege | `VipPrivilegeDomainService` | Annual-member coupon/benefit claims and the shared enable switch used by daily tasks |
| VIP points | `VipBigPointAppService`, `VipBigPointDomainService` | Sign-in, available task modules, supported browsing/watch activities and purchase tasks handled by the user |
| Live lottery | `LiveDomainService.TianXuan`, `CheckTianXuanDto.AwardNameIsSatisfied` | Eligible free lotteries, include/exclude precedence, denied anchors and grouping newly followed anchors |
| Fan medals | `LiveFansMedalTaskRunner`, `LiveFansMedalTaskPlanner`, `LiveMedalDashboardService` | Anchor exclusions, daily/custom budgets, lighting, savings limits, live-room conditions and per-medal progress |
| Batch unfollow | `AccountDomainService.UnfollowBatched` | Named group, reverse-order candidate selection, count and retained UIDs |
| Notification/recovery | `TaskFailureBatchMonitor`, `CookieTaskGuard`, `TaskRecoveryExecutor` | Daily cookie checks, per-account expiry reminder limit, scheduled-only failure summaries and recovery settings |

Two easily misread settings were checked through their full call paths. `ChargeTaskOptions.ChargeComment` generates a default comment when unset, even though the domain method does not contain the random selection. `VipBigPointOptions.ViewBangumis` is referenced by an unused private legacy watching method. The active `ogvwatchnew` task uses `CompleteV2Async`, so the old season-ID input is displayed as a disabled legacy setting with its saved value retained.

For batch unfollow, whitelisted candidates are skipped without selecting replacements. Descriptions therefore describe a maximum candidate count rather than promising exactly that many successful unfollows. The help does not execute transactions or platform tasks.

## Public references

- [BiliBiliToolPro task overview](https://github.com/RayWangQvQ/BiliBiliToolPro/blob/main/README.md): publisher-maintained task categories and purpose.
- [BiliBiliToolPro configuration reference](https://github.com/RayWangQvQ/BiliBiliToolPro/blob/main/docs/configuration.md): upstream setting terminology, checked against local code before use.
- [Bilibili annual membership benefits](https://www.bilibili.com/blackboard/activity-big-discount-m.html): official membership campaign and benefit terminology.
- [Bilibili fan-medal intimacy update](https://www.bilibili.com/opus/1100097465625870441): official fan-medal announcement, read alongside the earlier medal task research in `live-fans-medal-tasks.md`.
- [Bilibili live lottery interaction rules](https://live.bilibili.com/blackboard/era/e2AzTIVuoxiLHAu5.html): official explanation of live lottery participation and awards. This is contextual platform material, not a claim that every lottery format is supported by this build.

No production-account request, charge, donation, follow/unfollow or notification is needed to validate the help UI.
