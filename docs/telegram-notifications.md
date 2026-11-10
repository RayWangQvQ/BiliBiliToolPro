# Telegram 日志通知

Web 与 Console 宿主会兼容已有的 `TelegramBatched` 配置，在加载通知组件时自动启用分段发送。Bot Token、Chat ID、代理、批次和日志级别配置继续沿用。

长日志优先按换行拆分，每段为标题和换行预留容量。Unicode 字符保持完整，日志中的 HTML 特殊字符会转义。分段连续发送时至少间隔 1.1 秒。

正文拼接后保留完整日志。已有配置文件和数据库中的通知名称无需修改。

[Telegram Bot API 文档](https://core.telegram.org/bots/api#sendmessage)规定，解析格式实体后的消息长度最多为 4096 个字符。
