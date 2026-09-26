## 4.0.7
- Fix[#1144]: Web 面板启动失败时进程改为以非 0 退出码结束（catch 里记完 Fatal 日志后重新抛出，交由运行时按未处理异常终止）；此前顶层语句正常返回、退出码恒为 0，Docker / 青龙 / tencentScf 等编排层会把「启动崩溃」当成「正常退出」，既不重启也不告警
- 注意[#1144]: 存量部署里一直潜伏的配置错误会立刻显形为容器反复重启（这正是本次想要的效果）；退出码由运行时决定、与平台相关（Linux 134，Windows `0xE0434352`），不是稳定契约，排查请看容器日志里的 `Application terminated unexpectedly`
## 4.0.6
- **BREAKING[#1138]**: 部署平台目录统一迁移至 `platforms/` 下（`docker`、`podman`、`qinglong`、`baihu`、`daidai`、`helm`、`tencentScf`、`gitHubActions`、`krew`）；按 `raw.githubusercontent.com/.../main/<旧路径>` 直接拉取脚本或示例文件的存量部署会 404，请改用 `platforms/<平台>/...`
- Fix[#1140]: 修复 Web 面板启动时因模型与迁移不一致（`PendingModelChangesWarning`）而直接退出的问题；首次启动会自动为 `QRTZ_TRIGGERS`/`QRTZ_FIRED_TRIGGERS` 补列并创建 `QRTZ_PAUSED_JOB_GRPS` 表
- Fix[#1133]: 触发器进入 ERROR 状态时面板立刻刷新，补齐 Quartz 4 的 `OnTriggerInError`/`OnTriggersInError` 回调，不再要等下一轮轮询才发现
- 维护[#1133]: 升级到 Quartz.NET 4.1.1；作业存储的 System.Text.Json 实现 4.x 已并入 `Quartz` 核心，故删去独立的 `Quartz.Serialization.SystemTextJson` 引用
- 维护[#1125][#1131]: 依赖批量升级，含 MudBlazor 8.6.0→9.10.0、AppAny.Quartz.EntityFrameworkCore.Migrations.SQLite 0.6.0→0.6.1、Serilog、QRCoder、CronExpressionDescriptor、Ray.Infrastructure、bunit、xunit 等
- 维护[#1139][#1142]: 统一仓库文本文件换行符为 LF（`.bat`/`.cmd` 保留 CRLF），`.gitattributes` 只留兜底与例外声明，`.editorconfig` 去掉与 CSharpier 互相拉扯的 `insert_final_newline = false`；预览镜像触发路径收窄为仅工作流或配置文件变更
## 4.0.5
- Feature[#1106]: Web 新增「今日任务」页面：逐账号列出每个任务今天该不该做、做了没有，并提供单项/整账号/全部补做
- Feature[#1106]: 漏做的任务可自动补做，间隔与记录保留天数由 `AutoRecoverConfig` 配置（默认每 2 小时检查一次，文档见 `docs/configuration.md`）
- Feature[#1106]: Web 界面汉化（导航、首页、账号页、计划任务、日志/历史对话框、登录、修改密码、错误页）
- Fix[#1106]: 账号页与今日任务页首屏不再同步请求 B 站，改为本地数据先渲染、B 站状态随后并发补齐，并加 60 秒缓存与单账号超时
- Fix[#1106]: 账号页 Cookie 只显示截断值，不再把完整 Cookie 写进页面 DOM
- Fix[#1106]: WBI 签名判定误用 `w_rid`（该字段为空时被 Refit 从查询串丢弃，导致签名从未生效），改用必然存在的 `wts` 判定
- Fix[#1106]: 今日任务的到点计算改为按传入时刻的时区求值，不再依赖宿主机时区（原先在 UTC 机器上判为「本日无需执行」）
- 重构 GitHub Actions
- 新增 Dependabot（github-actions + nuget，目标分支 develop）
## 4.0.2
- 升级到dotnet10
- Fix[#1104]: WebApiClientCore 迁移到 Refit 后，参数首字母变为大写导致调用失败，现统一还原，并补充回归测试
- Feature: 响应解析失败时输出可定位的诊断日志，Cookie 等凭据一律掩码，避免进入日志与推送
- Feature[#1087]：适配呆呆面板（Daidai Panel）
- Fix: Bili Account 页面的增/改/删/排序写配置时，修正之前误用的环境变量式键名（`BiliBiliCookies__N`）
- Fix: Bili Account 页面的保存不再静默失败，成功/失败均给出 Snackbar 提示
## 4.0.1
- 新增Bili账号管理页面
- 重构Web
- 整合Agent Dtos
- 整合Agent interfaces
- WebApiClientCore迁移为Refit
- 重构AppService
- 升级bruno到v3
- bruno实现APP的sign签名算法和Web的WBI签名算法
- Refactor by GSD
- PR[#1078]：适配白虎面板
## 3.8.2
- Fix[#1026]: 更新文档
## 3.8.1
- Fix[#1005]: 更新文档
## 3.8.0
- Feature: 使推送的更版本号信息更简洁
- Fix[#998]: 修复企业微信 App 推送缺少 access_token 问题
- Fix[#996]: 修复 Server 酱推送标题不能为空的问题
- Fix[#669]: 企业微信默认消息类型从 markdown 改为 text
## 3.7.0
- Fix[#989]: 修复钉钉推送标题不能为空的问题
## 3.6.0
- Feature[#961]: Web 项目新增推送功能
- Feature[#961]: 升级 Serilog Pkg
- Feature: 使用中心化包管理
## 3.5.0
- Feature[#924]: 新增 Sqlite 配置源
- Feature[#924]: 新增在线配置页
- Feature[#924]: 根据任务拆分配置
- Feature[#924]: 实现开启、关闭任务功能
- Feature[#924]: 实现修改 Cron 定时时间功能
- Feature: 定时任务页改为默认50条
- Feature: 更新配置文档
## 3.4.0
- Feature: 优化登录失败时的提示信息
- Feature: 更新推送的文档说明
## 3.3.0
- Feature[#935]: Web 新增登录功能
- Feature[#935]: Web 新增修改密码功能
- Feature[#935]: 更新文档
- Feature: 更新开源协议为 GNU GPLv3
- Feature: 拆分原本的 Daily 任务
- Doc: 更新文档
- Feature: 升级 csharpier
- Feature: 变更默认数据库文件位置到 /app/config 下
## 3.2.0
- Fix: 修复大会员大积分签到任务
- Fix: 修复大会员大积分的签到和浏览追番频道任务
- Feature[#901]: 实现大会员大积分的浏览影视频道页任务
- Feature[#921]: 新增大会员大积分的观看剧集 bruno 信息
- Feature: 鉴权不再兼容老版本青龙（老版本需要手动添加 bili cookie）
- Feature: 修复 warnings
- Feature: 移除无用的using
- Fix: 修复 VerifyPR CI/CD 流水线
- Feature: README 添加 Trending 信息
## 3.1.0
- Feature[#842]: 对接青龙新的 OpenAPI，实现青龙版 Bili 登录后自动存储 Cookie
- Feature[#842]: 兼容老版本青龙的文件鉴权方式
- Feature[#842]: 新增新版青龙添加鉴权的说明文档
- Feature[#820]: 更新文档解决配置文件边缘场景下的刷新问题
- Fix[#863]: 修复青龙尝试修复异常任务
- Fix[#879]: 移除文档内过期的加速器地址
- Feature: 开启 Nullable 特性，在编译阶段检查潜在的 NullReferenceException 问题
- Feature: 临时取消 Null warning
- Fix: Bruno 脚本错误
- Fix: 尝试修复发布包时 CI/CD 丢失 change log 问题
## 3.0.0
- Feature[#884]: 上线 bili_tool_web
- Fix[#875]: 青龙检测 dotnet 版本只需要大于等于 8.0
- Fix[#876]: 升级 VipBigPoint 接口，解决风控
- Fix[#881]: 升级 LiveLottery 接口，解决风控
## 2.2.2
- Code refactor
- Integration Husky.Net and CSharpier
## 2.2.1
- Fix[#847]: DefaultRequestHeaders can not be null or empty with dotnet 8
- Fix[#849]: Temporary disable PublishTrimmed
## 2.2.0
- Migrate from dotnet 6.0 to dotnet 8.0
- Add Bruno to document the APIs
- Fix[#824]: Log cookie when qinglong save env failed
- Fix[#648]: Set DOTNET_SYSTEM_GLOBALIZATION_INVARIANT=1 in qinglong to ignore random "Couldn't find a valid ICU package" issue
## 2.1.3
- Code refactor
- Fix[#791]：修复VipBigPoint任务异常导致终止的问题
## 2.1.2
- Feature: enhancement CICD scripts
- Fix[#728]: compatible with qinglong history versions
## 2.1.1
- Feature: listen ctrl+c at the very beginning
- Fix: fix qinglong read cron error
## 2.1.0
- Feature[#691]: 重构并优化基于qinglong的部署方式，尝试解决偶发的安装失败的问题
- Feature[#670]: 新增针对App的AppUserAgent配置项，用于解决大会员大积分异常问题
- Fix: 修复CICD发布脚本错误
## 2.0.5
- Fix[#260]: 再次尝试修复大会员大积分“账号风险”异常
## 2.0.4
- Fix: 尝试修复大会员大积分“账号风险”异常
- Feature：为agent api创建集成测试
## 2.0.3
- PR[#641]：实现浏览会员购页面与观看正片内容功能
- PR[#685]：部分修复大积分功能
- Fix：更新过于老旧的UserAgent
- Fix：更新排行榜api
## 2.0.2
- PR[#617]：增加专栏投币功能与领取大会员经验的功能
## 2.0.1
- PR[#539]：更新文档
- PR[#557]：修复直播接口权限不足问题
## 2.0.0
- Feature[#513]：将基础组件和抽象迁移到nuget包中
- Fix[#543]：修复部分Api调用报“访问权限不足”
## 1.0.3
- Fix #486 : fix release zip error
## 1.0.2
- Fix #484 : fix read dic config error
- Merge PR #472 : add reverse proxy host for telegram notification
- Merge PR #483 : update login field for entrypoint
## 1.0.1
- Fix #463 : do not trust user's ck config
- Feature #460 : publish single file when release
- Feature: use new scripts for gh actions's release
- Feature #473 : let user input when there is no target task in configs
## 1.0.0
- Feature: Enable asynchronous
- Fix #344 : Support `Ctrl + C` to trigger exit event
- Fix #451 : Rebuild cookie factory pattern and fix bug of donating coin
- Featur: Replace AOP from MethodBoundaryAspect.Fody to Rougamo.Fody, to fix async exception
- Merge PR #448 : Fix typo
- Fix #446 : Change id type from int to long
## 0.4.6
- Fix: ck list init empty error
- Feature #440 : use 'apk add' to install dotnet in qinglong
## 0.4.5
- Fix #423 : Change int to string to avoid overflow exception
## 0.4.4
- Fix #228 : Try to fix sharing video error
- Feature: Change default docker image from dockerhub to github
## 0.4.3
- Feature #419 : Add a auto shell script for installing with docker
- Feature #396 : Publish docker image to GitHub pkg
## 0.4.2
- Merfe PRs #425 #426 #427 : Enhancement docker things, thx @zclkkk
## 0.4.1
- Merge PR #418 : Fix search video api's error, thx @catlair
## 0.4.0
- 合并PR（ #381 #383 ），新增直播间挂机功能，感谢@bakapiano
## 0.3.2
- Fix( #358 )，获取auth时兼容老版青龙文件路径
- Fix( #364 )，兼容青龙异形response数据类型
- Fix( #366 #361 )，修复一些低级bug
- Feature( #359 )，兼容读取不到`$QL_DIR`的情况
## 0.3.1
- Fix( #260 )，在需要的时候encode cookie
- 更新文档
## 0.3.0
- hotfix docker build error
- 合并PR（#341），新增krew部署，感谢@chenliu1993
- 合并PR（##348），更新文档，感谢@jexjws
- 合并PR（#350），修改请求header错误的bug，感谢@catlair
- 合并PR（#353），新增python扫码登录的feature（仅针对青龙），感谢@AFUL1991
- Feature（#351）：重构并新增了扫码登录功能，使之适用于各种部署平台
## 0.2.2
- 新增`podman`部署教程
- 合并PR（#264），腾讯云定时任务补充新增的大会员大积分任务，感谢@layui0320
- 合并PR（#262），更新docker的entry.sh，感谢@syrinka
- 合并PR（#308 #312），新增Chart部署，感谢@chenliu1993
- 合并PR（#309）新增lv6后开启白嫖模式的配置（多账号时可以实现不足lv6的继续投币，达到lv6的开始白嫖），感谢@cluom
- 优化青龙安装dotnet的脚本，改为使用官方`dotnet-install.sh`脚本安装（之前测试网络不通，后发现--no-cdn可以）
- 优化青龙的执行脚本，提取公共部分，并且在执行前会尝试安装一次dotnet，会清理一次缓存
## 0.2.1
- 合并PR（#253、#257），更新文档（@layui0320）
- 合并PR（#256），重构docker运行是cron构建方式，并优化读取环境变量的方式（@syrinka）
- Feature(#65)：新增TG推送配置并使用代理功能
- Feature(#240)：新增gotify推送
- Feature(#259)：大会员状态改为枚举类型，当非会员时自动跳过大积分任务
- Feature：更新、优化docker部署文档
## 0.2.0
- 新增大会员大积分任务
## 0.1.2
- 修复`auto-close-pr.yml`分支错误的bug
- 【#107】新增自动检测并关闭长时无状态issues的actions：no-response.yml
- 【#73】【#105】【#108】更新、纠正文档内容
- HostConfiguration，删除了CommandLine配置源，推荐只使用环境变量，同时更新青龙shell脚本内配置
- 【#169】领取大会员福利任务更改为每日都尝试执行
- 青龙拉库兼容大小写问题
- 【#197】合并PR，新增了阅读漫画功能到每日任务中（@ChanceLuo）
## 0.1.1
- 【#54】优化青龙shell脚本读取仓库目录方式，解决青龙新老版本切换导致出现多个repo目录的bug
- 【#82】【#85】合并外部PR，更新了文档
- 感谢`JetBrain`提供免费的证书支持
## 0.1.0
- 【#62】`codeql-analysis.yml`可以指定检查的文件类型
- 【#61】`publish-image.yml`手动打镜像时支持指定是否打latest的tag
- 【#32】新增企业微信的应用推送，实现微信接受推送消息
- 优化日志格式
## 0.0.9
- 【#47】青龙安装`dotnet`环境，支持arm架构服务器
## 0.0.8
- 【#55】新增日志推送端：`Microsoft Teams`
- 【#27】更新README
## 0.0.7
- 【#44】兼容青龙最新版本（v2.12.0），修复因青龙调整目录结构导致的bug
- 更新`publish-image.yml`，只有`release`时才打`latest tag`，手动运行时不打`latest tag`
## 0.0.6
- 更新docker镜像的构建
- 【#12】新增配置`Notification:IsSingleAccountSingleNotify`，支持开启每个账号单独推送消息
- publish-release.yml新增手动输入tag功能
## 0.0.5
- 优化推送日志，在标题中显示运行的任务名称
- 新增`CodeQL`workflows，用于检测代码
- 新增`Publish image`workflows，用于发布镜像
- 新增`no-toxic-comments.yml`，用于检测评论
- 更新`auto-close-pr.yml`，用于修正PR的目标到`develop`
## 0.0.4
- 【#15】修复`Actions`部署到腾讯云函数时的偶发异常
## 0.0.3
- 【#16】修复银瓜子兑换硬币bug
- 【#18】修改[青龙面板](https://github.com/whyour/qinglong)以`Production`环境运行
- [青龙面板](https://github.com/whyour/qinglong)新增拉取dev先行版功能
## 0.0.2
- 更新文档
- 天选抽奖新增黑名单功能
- 批量取关新增白名单功能
## 0.0.1
- 重启项目
- 支持[青龙面板](https://github.com/whyour/qinglong)部署
