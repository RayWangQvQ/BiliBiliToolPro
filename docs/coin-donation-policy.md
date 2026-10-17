# 按等级停止投币

`DailyTaskConfig:CoinDonationStopLevel` 设置停止投币的账号等级，范围为 1–6。每个账号单独判断，达到该等级后跳过视频和专栏投币，其他任务继续执行。补做也遵循此等级限制。

Web 配置页选择“不按等级停止”后继续投币。旧配置 `SaveCoinsWhenLv6=true` 仍按 Lv.6 停止，新设置大于 0 时优先使用新等级。
