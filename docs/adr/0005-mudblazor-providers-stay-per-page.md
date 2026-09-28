# ADR-0005：MudBlazor 四个 Provider 逐个页面声明，不上提到 App.razor / MainLayout

- 状态：已接受
- 日期：2026-09-28
- 关联：`src/Ray.BiliBiliTool.Web/Components/Pages/Today/Today.razor`

## 背景

面板的界面组件库是 MudBlazor 9.10。用到 `MudThemeProvider` / `MudPopoverProvider` / `MudDialogProvider` / `MudSnackbarProvider` 四件套，页面才拿得到主题变量、弹层、对话框与提示条。

这四件套目前是**每个交互页面各写一份**（13 个页面末尾都有），不是上提到 `App.razor` 或 `MainLayout.razor`。这个约定是靠抄写维持的，没有任何文档或检查兜底——于是今日任务页成了唯一的漏网之鱼：

- 11 处 `Snackbar.Add(...)` 一个都不弹，操作「成功 / 失败」完全没有反馈；
- 页面内联 `<style>` 引用的 `var(--mud-palette-*)` 取不到值，状态色全部落回默认黑；
- `Class="mud-text-secondary"` 无效。

漏掉四件套的**症状是静默的**：不报错、不崩、页面照常渲染，只是「提示条不出现、颜色不对」。这正是它能在代码里躺这么久的原因。

## 决策

四件套继续逐个页面声明，放在页面 `.razor` 末尾。**新增交互页面时必须照抄这四行。**

## 为什么不上提

`Routes.razor` 没有设全局 render mode，各页面自己声明 `@rendermode InteractiveServer`（`Ray.BiliBiliTool.Web.Client` 的 `Counter.razor` 用 `InteractiveAuto`）。因此 `App.razor` 与 `MainLayout.razor` 属于**静态 SSR 渲染树**。

MudBlazor 的 Provider 必须在交互渲染树内实例化：它们靠订阅 `ISnackbar` / `IDialogService` 这类 scoped 服务来工作，静态树里渲染出来的那份和页面交互电路里的不是同一个实例，Provider 收不到事件。上提的后果是主题色能生效、但 Snackbar / Dialog / Popover **全体失效**——用一个静默问题换另一个静默问题，而且波及全部 13 个页面。

要安全地统一，前提是先把面板改成全局交互（`App.razor` 里 `<Routes @rendermode="InteractiveServer" />`），这会和 `InteractiveAuto` 的 Client 页面打架，且需要对全部页面做回归验证。本次不做。

## 后果

- 新增/复制页面时漏写四件套仍然可能，且失败是静默的。靠本 ADR + CONTEXT.md 记录来降低概率，不引入自动化检查。
- `NavMenu.razor` 自己声明了 `@rendermode InteractiveServer` 且不使用这四件套，不受影响。
- 若将来面板迁到全局交互或 `MudLayout` 体系，本 ADR 作废，四件套应一次性上提到 `MainLayout.razor` 并删除各页面内的声明。

## 备选方案

**A. 上提到 `App.razor`，并从 13 个页面删掉重复声明。**
单看代码更干净，也是 MudBlazor 官方推荐形态。未采纳：在当前「每页各自 InteractiveServer」的架构下会让 Snackbar / Dialog / Popover 全体失效，见上。

**B. 抽一个 `<MudProviders/>` 组件，各页面引用它。**
把「四行」收敛成「一行」，但落不落仍然是每页自己负责，静默失败的概率没有本质变化，却多一个间接层。未采纳。
