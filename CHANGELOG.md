## 0.0.1
- 重启项目
- 支持[青龙面板](https://github.com/whyour/qinglong)部署
## 0.0.2
- 更新文档
- 天选抽奖新增黑名单功能
- 批量取关新增白名单功能
## 0.0.3
- 【#16】修复银瓜子兑换硬币bug
- 【#18】修改[青龙面板](https://github.com/whyour/qinglong)以`Production`环境运行
- [青龙面板](https://github.com/whyour/qinglong)新增拉取dev先行版功能
## 0.0.4
- 【#15】修复`Actions`部署到腾讯云函数时的偶发异常
## 0.0.5
- 优化推送日志，在标题中显示运行的任务名称
- 新增`CodeQL`workflows，用于检测代码
- 新增`Publish image`workflows，用于发布镜像
- 新增`no-toxic-comments.yml`，用于检测评论
- 更新`auto-close-pr.yml`，用于修正PR的目标到`develop`
## 0.0.6
- 更新docker镜像的构建
- 【#12】新增配置`Notification:IsSingleAccountSingleNotify`，支持开启每个账号单独推送消息
- publish-release.yml新增手动输入tag功能
## 0.0.7
- 【#44】兼容青龙最新版本（v2.12.0），修复因青龙调整目录结构导致的bug
- 更新`publish-image.yml`，只有`release`时才打`latest tag`，手动运行时不打`latest tag`
## 0.0.8
- 【#55】新增日志推送端：Microsoft Teams
- 【#27】更新README
## 0.0.9
- 【#47】青龙安装dotnet环境，支持arm架构服务器
## 0.0.10
- 【#62】codeql-analysis.yml可以指定检查的文件类型
- 【#61】publish-image.yml手动打镜像时支持指定是否打latest的tag
