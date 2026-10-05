# 后台每日任务随机延迟

`DailyTaskConfig:RandomDelayMaxMinutes` 设置后台定时每日任务的随机延迟上限，范围为 0–1440 分钟。留空继承 `Security:RandomSleepMaxMin`，填写 0 立即执行。手动执行和补做即时执行。
