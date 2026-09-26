# ADR-0001：Web 面板启动失败必须以非 0 退出码结束

- 状态：已接受
- 日期：2026-09-26
- 关联：issue [#1144](https://github.com/RayWangQvQ/BiliBiliToolPro/issues/1144)、PR [#1140](https://github.com/RayWangQvQ/BiliBiliToolPro/pull/1140)、PR [#1133](https://github.com/RayWangQvQ/BiliBiliToolPro/pull/1133)

## 背景

`Ray.BiliBiliTool.Web` 的宿主入口点用最外层 `try/catch` 包住整个启动流程。catch 只写一条 Fatal 日志，随后顶层语句正常返回，**进程退出码是 0**。除了翻日志，没有任何机制能发现「面板其实没起来」。

后果：

- Docker（`restart: unless-stopped`）、青龙、tencentScf 等编排层只看退出码，把启动崩溃当成正常退出：不重启、不告警，端口从未监听。
- CI 与宿主集成测试无法靠退出码发现启动失败。
- 大版本升级时最危险：真正「升级后启动不了但看起来是干净退出」的场景容易被归因到别处。PR #1140 就是这种情况 —— 模型与迁移不一致触发 `PendingModelChangesWarning`，启动即抛，但表面症状只是「面板起不来」。

已实测复现（Windows，非法 `ConnectionStrings__Sqlite`）：进程打印 `[FTL] Application terminated unexpectedly` 与完整栈，然后**退出码为 0**。

## 决策

catch 里保留 Fatal 日志，flush 仍交给 `finally`，然后 **`throw;`** —— 把异常重新交给运行时，由运行时按「未处理异常」的默认方式终止进程，退出码自然非 0。

`Ray.BiliBiliTool.Console` 早已是这个形状（`Main` 返回 `1`），Web 此前是唯一的例外。

## 后果

- 编排层重新能看见启动失败：容器会反复重启（`restart: unless-stopped`）、K8s 会进入 `CrashLoopBackOff`、CI 步骤会失败。
- **存量部署里一直潜伏的配置错误会立刻显形为容器反复重启。** 这是本次想要的效果。
- **退出码不是稳定契约**：它由运行时按未处理异常决定，与平台相关 —— Linux 上为 `134`（SIGABRT），Windows 上实测为 `0xE0434352`。ADR 只能约定「非 0」，不能约定具体数值；排查仍需看日志里的 `Application terminated unexpectedly`。
- **异常栈会打印两次**：一次是 Serilog 的 Fatal，一次是运行时的 `Unhandled exception.`。
- `finally` 里的 `Log.CloseAndFlush()` 在栈展开时先于运行时输出执行，SQLite sink 的批量日志不会丢。

## 备选方案

**A. `Environment.ExitCode = 1;`，不重新抛出。**
退出码稳定为 `1`、栈只打一次，是更可文档化、可测试的契约。未采纳，但它是「想要稳定退出码」时唯一的可行路径。

**A′. `Environment.ExitCode = 1; throw;`（两者兼得）。**
**不可行，已实测证伪**：运行时在未处理异常终止进程时会覆盖 `Environment.ExitCode`。Windows 上实测最终退出码仍是 `0xE0434352`，而不是 `1`。

**B. 直接删掉这层 `try/catch`，让未处理异常走默认路径。**
能拿到非 0 退出码，但会丢掉 `Log.CloseAndFlush()`：SQLite sink 与各推送 sink 是批量写入的，启动阶段已缓冲的日志会丢，而启动失败恰恰是最需要这些日志的时候。未采纳。

## 不在范围内

- **不为退出码加自动化守卫测试。** issue 建议给 `Host.IntegrationTests` 加一条守卫，但集成测试用的是**进程内** `WebApplicationFactory<Program>`，它走不到「进程退出码」这条路径；唯一有效的写法是启动真实子进程，本次不做。
- **不加 HEALTHCHECK / k8s liveness、readiness、startup probe。** 探针解决的是「进程活着但没就绪」，与「启动即崩」是两回事，属于另一个议题。
- **不与 Quartz 升级同发布。** Quartz 4.1.1 已随 4.0.6 发布，本改动单独进 4.0.7，避免故障归因被搅混、回滚只能整体回滚。
