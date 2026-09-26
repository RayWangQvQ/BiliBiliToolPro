# 术语表

本项目内部反复出现、且容易各说各话的词。按需增补，只收真正会歧义的。

| 术语 | 含义 | 不要叫它 |
| --- | --- | --- |
| **面板** / **Web 面板** | `Ray.BiliBiliTool.Web` 这个宿主：Blazor 界面 + 调度 + 配置存储 | 「网站」「前端」「UI」（它不只是界面，启动它同时会拉起 Quartz 与 EF 迁移） |
| **宿主入口点** | 一个宿主进程的顶层启动代码：Web 是 `src/Ray.BiliBiliTool.Web/Program.cs` 的顶层语句，Console 是 `src/Ray.BiliBiliTool.Console/Program.cs` 的 `Main` | 「启动类」「bootstrap」 |
| **编排层** | 负责拉起、重启、告警宿主进程的外部系统：Docker / podman、青龙、白虎、呆呆、tencentScf、Helm / K8s、GitHub Actions | 「运维」「部署脚本」（指的是**外部**那一层，不是仓库里的 `platforms/`） |
| **启动失败** | 宿主在进入正常服务状态之前抛出的异常：从 `WebApplication.CreateBuilder` 到 `app.Run()` 之内都算 | 「崩溃」（运行时崩溃是另一回事）、「启动慢」 |
| **吞异常** | catch 里只记日志、却不改变进程结果的写法：调用方拿到的仍是「成功」 | 「捕获异常」（捕获本身没问题，吞掉结果才是问题） |

## 相关

- [ADR-0001：Web 面板启动失败必须以非 0 退出码结束](adr/0001-web-startup-failure-must-exit-nonzero.md)
