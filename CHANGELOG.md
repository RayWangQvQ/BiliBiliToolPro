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
