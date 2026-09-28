# BiliBiliToolPro

B 站每日任务自动化工具：多宿主（Web 面板 / Console / 各类云函数）+ 平台层编排。本文件是项目共享术语表，写 issue、提重构、命名变量时优先用这里的词。

## Language

**面板** / **Web 面板**:
`Ray.BiliBiliTool.Web` 这个宿主：Blazor 界面 + 调度 + 配置存储。启动它同时会拉起 Quartz 与 EF 迁移，不只是界面。
_Avoid_: 网站、前端、UI

**宿主入口点**:
一个宿主进程的顶层启动代码：Web 是 `src/Ray.BiliBiliTool.Web/Program.cs` 的顶层语句，Console 是 `src/Ray.BiliBiliTool.Console/Program.cs` 的 `Main`。
_Avoid_: 启动类、bootstrap

**编排层**:
负责拉起、重启、告警宿主进程的外部系统：Docker / podman、青龙、白虎、呆呆、tencentScf、Helm / K8s、GitHub Actions。指的是**外部**那一层，不是仓库里的 `platforms/`。
_Avoid_: 运维、部署脚本

**启动失败**:
宿主在进入正常服务状态之前抛出的异常：从 `WebApplication.CreateBuilder` 到 `app.Run()` 之内都算。运行时崩溃是另一回事。
_Avoid_: 崩溃、启动慢

**吞异常**:
catch 里只记日志、却不改变进程结果的写法：调用方拿到的仍是「成功」。捕获本身没问题，吞掉结果才是问题。
_Avoid_: 捕获异常

**应用版本**:
面板上给用户看的那一项版本信息（侧边栏底部与 `/about`）：CI 产物显示版本号（如 `4.0.8-alpha.3`），非 CI 产物显示 `开发版`。载体是宿主程序集的 InformationalVersion（ADR-0003）。
_Avoid_: 版本号（在仓库里专指 `x.y.z` 那三段）、版本信息、程序版本

**alpha 版本**:
PR 合入主干后由 CI 自动产出的预览版本，形如 `x.y.z-alpha.N`（N 自上个稳定 tag 起从 1 递增）；只推 Docker 镜像，用后即弃，其 x.y.z 基底是"暂定下一 patch"，与最终稳定版号可能错配。
_Avoid_: 预览版、beta、开发版

**稳定版**:
维护者手动触发发版 workflow、选定 patch/minor/major 后由 CI 算号并发布的正式版本：打纯数字三段式 git tag、建 GitHub Release、出全套制品。
_Avoid_: 正式版、release 版

## 相关

- [ADR-0001：Web 面板启动失败必须以非 0 退出码结束](docs/adr/0001-web-startup-failure-must-exit-nonzero.md)
- [ADR-0002：单主干 + CI 托管版本号](docs/adr/0002-ci-managed-versioning-trunk-based.md)
- [ADR-0003：应用版本以程序集 InformationalVersion 为唯一载体](docs/adr/0003-app-version-carrier-and-display.md)
