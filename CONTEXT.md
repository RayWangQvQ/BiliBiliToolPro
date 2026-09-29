# BiliBiliToolPro

B 站每日任务自动化工具：多宿主（Web 面板 / Console / 各类云函数）+ 平台层编排。本文件是项目共享术语表，写 issue、提重构、命名变量时优先用这里的词。

## Language

**面板** / **Web 面板**:
`Ray.BiliBiliTool.Web` 这个宿主：Blazor 界面 + 调度 + 配置存储。启动它同时会拉起 Quartz 与 EF 迁移，不只是界面。
_Avoid_: 网站、前端、UI

**面板管理员**:
登录面板用的那个账号，在 `/Admin` 页维护。整个面板**只有一个**（`bili_user` 表 `Id=1`，角色 `Administrator`，默认用户名 `admin`），登录名与密码是两个可独立变更的属性——改一个不会顺带改另一个（ADR-0009）。
_Avoid_: 用户、账户、账号、管理账户

**B 站账号**:
用户自己的 B 站身份，在 `/BiliAccount` 页管理，一个面板可以挂多个，靠扫码登录。与面板管理员是两回事：前者是「工具替谁干活」，后者是「谁在用工具」。
_Avoid_: 账号、账户、用户

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

**检查项**:
今日任务页上的**二级**条目、一行的状态单元（如「观看视频」「投币」），对应 `TaskItemDefinition`，展示态是 `TodayTaskItemDto`。归属于一个任务。
_Avoid_: 任务项、条目、子任务

**任务**:
今日任务页上的**一级**条目（如「每日任务」「充电」「批量取关」），对应 `TaskDefinition`（`taskKey`），补做时按它定位。一个任务下辖一到多个检查项：只有「每日任务」下辖多项，其余都是**整任务算一项**（检查项的 `ItemKey` 为 null，名字与任务名相同）。`TodayTaskGroupDto` 只是「任务 + 其检查项」在页面上的承载结构，不是独立概念。
_Avoid_: 任务分组、分类、类别、模块

**漏做**:
今天该做、但到检查时仍未完成的检查项（`NotDone`）。与「失败」是两回事——失败是跑过但没成功。
_Avoid_: 没做、未完成、缺失

**补做**:
对某个检查项重新执行一次任务。分手动（用户在今日任务页点「补做」）与自动（`AutoRecoverJob` 触发）两种，靠 `TaskRecordTrigger` 区分——每日自动补做次数上限统计的就是 `Trigger=Auto` 的记录，传错会导致上限失效。
_Avoid_: 重试、重跑、恢复

## 相关

- [ADR-0001：Web 面板启动失败必须以非 0 退出码结束](docs/adr/0001-web-startup-failure-must-exit-nonzero.md)
- [ADR-0002：单主干 + CI 托管版本号](docs/adr/0002-ci-managed-versioning-trunk-based.md)
- [ADR-0003：应用版本以程序集 InformationalVersion 为唯一载体](docs/adr/0003-app-version-carrier-and-display.md)
- [ADR-0004：镜像构建跑在宿主机架构上，不用 QEMU 模拟 arm64](docs/adr/0004-image-cross-build-on-buildplatform.md)
- [ADR-0005：MudBlazor 四个 Provider 逐个页面声明，不上提到 App.razor / MainLayout](docs/adr/0005-mudblazor-providers-stay-per-page.md)（已被 ADR-0006 作废）
- [ADR-0006：Web UI 全站 MudBlazor 化 + 自定义深空蓝主题 + 暗色模式](docs/adr/0006-web-ui-mudblazor-native-theme.md)
- [ADR-0007：面板窄屏改用响应式抽屉，侧栏宽度必须带 CSS 单位](docs/adr/0007-web-narrow-viewport-drawer.md)
- [ADR-0008：Blazor 框架脚本必须显式声明 `RequiresAspNetWebAssets` 随包发布](docs/adr/0008-blazor-framework-assets-must-be-published.md)
- [ADR-0009：面板管理员的改名与改密是两个独立操作](docs/adr/0009-admin-rename-and-password-change-are-separate.md)
