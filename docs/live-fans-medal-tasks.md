# Live fan medal tasks

The Web panel supports two execution goals:

- Custom: retain the configured number of likes, messages and watch minutes per medal.
- Follow daily tasks: read each medal's current requirements and remaining progress from Bilibili.

Likes, messages and watching have independent switches. A zero custom budget disables that action. Existing installations retain their custom budgets by default. The obsolete `IsSkipLevel20Medal` key is ignored. All medal levels can participate in the tasks returned by Bilibili.

The runner loads the paginated fan medal panel, deduplicates anchors, and reads `GetActivatedMedalInfo`. The task title supplies the interaction quantity or watch minutes per round. The progress supplies completed and total rounds. Completed tasks and medals with full free-intimacy savings skip upgrade interactions. Unlit medals use the returned lighting requirements.

Likes run only while the anchor is live and are sent in batches of up to ten. Messages can be restricted to offline rooms. Unlit live medals use likes for lighting when likes are enabled. Requests are paced and progress is refreshed after a round. A lighting task is refreshed before daily upgrade tasks start. Each account has a local ceiling of 5,000 accepted likes per execution. Follow mode also limits each medal to 100 messages and 24 watch hours per execution.

Watch sessions use a stable device identifier, honor the returned heartbeat interval, and count successful timed heartbeats without counting the initial room-entry request. Sessions stop when the anchor goes offline or the returned task is complete. Watch operations accept cancellation. API failures and unconfirmed completion are recorded as task failures.

## Configuration

All keys are under `LiveFansMedalTaskConfig`:

| Key | Default | Meaning |
| --- | --- | --- |
| `FollowDailyTaskLimit` | `false` | Use the current remaining daily task amount |
| `EnableLike` | `true` | Enable likes |
| `EnableDanmaku` | `true` | Enable messages |
| `EnableWatch` | `true` | Enable watch sessions |
| `DanmakuOnlyWhenOffline` | `false` | Send messages only when the anchor is offline |
| `ExcludedAnchorIds` | Empty | Comma-separated anchor UIDs excluded from all medal interactions across accounts |
| `LikeNumber` | `30` | Custom likes per medal |
| `SendDanmakuNumber` | `1` | Custom messages per medal |
| `HeartBeatNumber` | `70` | Custom watch minutes per medal |

## Research

[Bilibili Live's official medal announcement](https://www.bilibili.com/opus/1100097465625870441), edited August 12, 2025, announced unified levels 1–120. Its watch rewards describe that announcement and are not used as fixed 2026 requirements.

[BLTH's maintained task implementation](https://github.com/andywang425/BLTH/tree/master/src/modules/dailyTasks/liveTasks/medalTasks) and [release history](https://github.com/andywang425/BLTH/releases) document the May–September 2026 task adaptations. They provide implementation evidence for the current task APIs, dynamic progress, heartbeat timing and duplicate panel entries. Actual quantities are read from Bilibili for each medal at execution time.

Verification uses synthetic cookies, mocked APIs, independent signature vectors and panel component tests. It sends no live messages, likes, watch heartbeats or notification test pushes.

## 主播白名单

默认对全部粉丝牌执行任务，可在卡片上勾选排除主播。开启“仅为白名单主播执行任务”后，只有加入白名单的主播参与，空白名单暂停全部粉丝牌任务。排除设置优先生效。

选择对面板中所有 B 站账号生效，切换账号后保留此前选择。修改后点击“保存配置”生效。配置键为 `OnlySelectedAnchors`、`IncludedAnchorIds` 和 `ExcludedAnchorIds`，ID 列表使用逗号分隔。
