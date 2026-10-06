# Live fan medal tasks

The Web panel supports two execution goals:

- Custom: retain the configured number of likes, messages and watch minutes per medal.
- Follow daily tasks: read each medal's current requirements and remaining progress from Bilibili.

Likes, messages and watching have independent switches. A zero custom budget disables that action. New installations use remaining daily quotas by default. Stored custom budgets remain available in timed execution mode. The obsolete `IsSkipLevel20Medal` key is ignored. All medal levels can participate in the tasks returned by Bilibili.

The runner loads the paginated fan medal panel, deduplicates anchors, and reads `GetActivatedMedalInfo`. The task title supplies the interaction quantity or watch minutes per round. The progress supplies completed and total rounds. Completed tasks and medals with full free-intimacy savings skip upgrade interactions. Unlit medals use the returned lighting requirements.

Likes run only while the anchor is live and are sent in batches of up to ten. Messages can be restricted to offline rooms. Unlit live medals use likes for lighting when likes are enabled. Requests are paced and progress is refreshed after a round. A lighting task is refreshed before daily upgrade tasks start. Timed runs limit each account to 5,000 accepted likes per execution. Monitoring shares a 5,000-like daily reservation ceiling across anchors for each account in the running process. Follow mode also limits each medal to 100 messages and 24 watch hours per execution.

Watch sessions require a lit medal and valid parent and child room areas, but do not require the anchor to be live. They use a stable device and room-area identity, honor the returned heartbeat interval, and count successful timed heartbeats without counting room entry. Going offline does not interrupt a session. Sessions stop at the configured quota, confirmed platform completion, cancellation or the failure limit. Watch operations accept cancellation. Follow-daily runs report unconfirmed completion as a task failure.

## Configuration

All keys are under `LiveFansMedalTaskConfig`:

| Key | Default | Meaning |
| --- | --- | --- |
| `UseLiveStateMonitoring` | `true` | Check live state and start eligible unfinished actions |
| `MonitorIntervalMinutes` | `5` | Minutes between live-state checks, from 1 to 30 |
| `FollowDailyTaskLimit` | `true` | Timed mode can follow the current remaining daily task amount |
| `DailyLikeNumber` | `300` | Editable daily like limit per medal in live-state monitoring mode |
| `DailyDanmakuNumber` | `10` | Editable daily message limit per medal in live-state monitoring mode |
| `DailyWatchMinutes` | `150` | Editable daily watch minutes per medal in live-state monitoring mode |
| `EnableLike` | `true` | Enable likes |
| `EnableDanmaku` | `true` | Enable messages |
| `EnableWatch` | `true` | Enable watch sessions |
| `DanmakuOnlyWhenOffline` | `false` | Send messages only when the anchor is offline |
| `ExcludedAnchorIds` | Empty | Comma-separated anchor UIDs excluded from all medal interactions across accounts |
| `PinnedAnchorIds` | Empty | Comma-separated anchor UIDs pinned in the Web display across accounts |
| `LikeNumber` | `30` | Custom likes per medal |
| `SendDanmakuNumber` | `1` | Custom messages per medal |
| `HeartBeatNumber` | `70` | Custom watch minutes per medal |

## Research

[Bilibili Live's official medal announcement](https://www.bilibili.com/opus/1100097465625870441), edited August 12, 2025, announced unified levels 1–120. Its watch rewards describe that announcement and are not used as fixed 2026 requirements.

[BLTH's maintained task implementation](https://github.com/andywang425/BLTH/tree/master/src/modules/dailyTasks/liveTasks/medalTasks) and [release history](https://github.com/andywang425/BLTH/releases) document the May–September 2026 task adaptations. They provide implementation evidence for the current task APIs, dynamic progress, heartbeat timing and duplicate panel entries. Actual quantities are read from Bilibili for each medal at execution time.

The latest [watch-task source](https://github.com/andywang425/BLTH/blob/6b72f4138c1e2a34b5aafab171f314863d769d57/src/modules/dailyTasks/liveTasks/medalTasks/watchTask.ts), checked October 6, 2026, validates both room area identifiers and sends entry and periodic heartbeats without a target-room live-state condition. Its wait for the current page's player to go offline is distinct from target-room eligibility. It verifies progress after sending heartbeats. This is implementation evidence rather than a guarantee that every account or room will receive credit.

Verification uses synthetic cookies, mocked APIs, independent signature vectors and panel component tests. It sends no live messages, likes, watch heartbeats or notification test pushes.

## 主播白名单

默认对全部粉丝牌执行任务，可在卡片上勾选排除主播。开启“仅为白名单主播执行任务”后，只有加入白名单的主播参与，空白名单暂停全部粉丝牌任务。排除设置优先生效。

选择对面板中所有 B 站账号生效，切换账号后保留此前选择。排除或恢复主播会先弹出确认框，确认后立即保存并更新排序。取消时保留原来的参与状态和顺序。此操作只保存排除名单，其他配置和白名单仍需点击“保存配置”。配置键为 `OnlySelectedAnchors`、`IncludedAnchorIds` 和 `ExcludedAnchorIds`，ID 列表使用逗号分隔。

## 粉丝牌列表

置顶主播优先展示，已排除主播始终排在列表末尾。同组内按粉丝牌等级从高到低排列。置顶只调整展示顺序，修改后点击“保存配置”保存。置顶名单使用 `PinnedAnchorIds`，对所有账号生效。

列表默认每页展示 6 位主播，可切换为 12 或 24 位。搜索在全部粉丝牌中查找，切换搜索、账号或每页数量时回到第一页。

点击页码或前后页按钮后，分页按钮保持在切换前的屏幕位置，便于连续翻页。最后一页主播较少时也保持分页位置。搜索、切换账号、调整每页数量及后台进度更新不会主动滚动页面。

页面先展示上次成功读取的数据，再自动更新今日进度。刷新期间保留已有卡片，读取完成后更新界面。缓存保存在持久化的 `config/live-medal-dashboard-cache/` 目录，按登录凭据的摘要隔离账号，保留最近 7 天的数据。重新登录使用新的缓存，主播选择与置顶仍由已保存的配置决定。

打开页面后，后台每轮点赞、弹幕和观看任务读取到的实际进度会自动显示。页面每分钟再核对一次最新数据，保留账号选择、搜索、分页和未保存的配置。切换账号后只接收当前账号的更新，离开页面后停止页面查询。

## 今日任务完成状态

今日任务从 B 站读取参与主播的粉丝牌进度。任务执行成功记录只表示该次执行正常结束。已排除主播和白名单之外的主播不计入完成情况。自动模式和定时自定义模式均核对动作开关及配置次数是否大于零。

待点亮粉丝牌以实际点亮状态为准，点赞与弹幕是点亮的可选方式。已点亮粉丝牌按已开启任务的完成标记核对，亲密度储蓄已满时无需继续升级互动。未开播或弹幕时段限制使全部剩余任务无法执行时，显示等待任务条件。没有参与主播或可执行任务时，显示当前无需执行。进度尚未获取时显示状态未知。

首次打开页面先显示检测中，读取今日进度后显示完成数量和待点亮数量。历史缓存用于粉丝牌列表预览，不作为今日任务完成依据。定时自定义次数模式当天已有成功执行记录时不继续自动补做，手动补做仍可使用。定时跟随每日任务模式可在剩余任务满足执行条件时自动补做。按主播状态自动执行时由后台检查处理剩余任务。

## 各动作状态

粉丝牌卡片根据每日进度、动作开关和主播状态分别显示点赞、弹幕、观看任务的状态。尚未执行时显示待点赞、待发弹幕或待观看，有进度但未完成时显示对应动作未达标。点赞需要开播，未开播时显示待开播。观看不等待开播，粉丝牌未点亮时显示等待点亮。仅离线弹幕在直播期间显示待下播。直播中通过点赞点亮粉丝牌时，弹幕显示等待点亮。

已完成的每日任务显示已完成，已经点亮的点亮任务显示已点亮。未开启的动作显示已关闭，排除主播和白名单之外的主播显示已排除或未参与。亲密度储蓄已满的升级任务显示无需执行。历史或过时的缓存显示待刷新，仍保留上次读取的进度。进度达到目标而完成标记尚未更新时显示待确认完成。

状态展示每个动作的目标进度和执行条件。

## 按主播状态自动执行

默认每 5 分钟检查参与主播的当前直播状态和当日任务进度，可选择 1～30 分钟。点赞在开播后执行，已点亮粉丝牌的观看任务无需等待主播开播。开启仅离线弹幕时，弹幕在下播后的检查中执行，未勾选时也可在直播期间发送。

每个粉丝牌按用户设置的每日上限执行，并读取 B 站实际任务进度。任务卡片展示平台任务剩余的点赞次数、弹幕条数和观看分钟数。达到用户设置的额度或完成平台任务后停止对应互动。观看保持连续心跳，同一账号的观看任务依次执行。其他主播开播后，点赞和弹幕仍可照常启动。手动执行和后台检查共享动作锁，同一账号、主播和动作不会重复执行。

关闭任务、移出白名单、排除主播或更新登录凭据后会停止相关后台任务。跨日重新读取每日额度，失败动作等待 15 分钟后重试，失败通知沿用现有汇总设置。自动检查不消耗今日任务的自动补做次数。

关闭“按主播状态自动执行”后使用原有执行时间。自动模式的每日额度和定时自定义模式的每次额度分别保存，切换模式后保留各自的次数和时长。定时触发器在自动状态模式下暂停，切回定时模式并保存后恢复。

## 可编辑的每日互动额度

自动模式的默认值为点赞 300 次、弹幕 10 条、观看 150 分钟，保存后使用用户设置的数值。平台任务进度仍按 B 站实际返回的数据展示。例如当天已完成 210 次点赞，设置每日 240 次后最多再执行 30 次。设置为零会暂停对应动作。

每个账号、主播和动作分别记录当天已执行数量。记录持久化在 `config/live-medal-daily-usage.json`，部分任务轮次和重启前的执行次数也计入额度。观看按平台心跳间隔累计，在剩余时长不足一次心跳时停止。额度在 UTC+8 日期变更后重新计算。用户增大设置后可继续执行，调小后使用新的上限。

自动模式使用新的每日设置。旧定时模式的自定义次数和时长仍保留，旧的跟随每日任务量选项仅用于定时模式。

## 观看执行顺序

同一账号每次只观看一个直播间，其他主播依次排队。当前会话结束后，下一位主播会先核对最新任务进度，已完成的观看任务直接跳过。自动监测、定时任务和手动补做共用这一规则。不同账号独立执行，点赞与弹幕照常进行。
