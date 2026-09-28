# ADR-0006：Web UI 全站 MudBlazor 化 + 自定义深空蓝主题 + 暗色模式

- 状态：已接受
- 日期：2026-09-28
- 关联：作废 [ADR-0005](./0005-mudblazor-providers-stay-per-page.md)；`src/Ray.BiliBiliTool.Web/Components/Layout/MainLayout.razor`、`NavMenu.razor`、`App.razor`

## 背景

`Home.razor` 重做后确立了 MudBlazor 原生视觉标杆（`MudContainer` + `MudGrid` + `MudCard`）。但全站仍有一处明显的「风格断层」：

- 外层骨架 `MainLayout.razor` 还是 Blazor 默认模板——`.sidebar`（250px 深蓝紫渐变）+ `.top-row`（灰顶栏）+ bootstrap 类（`navbar` / `nav-link` / `bi` 图标）。
- `wwwroot/app.css` 残留模板色：链接 `#006bb7`、`.btn-primary` `#1b6ec2`，与 MudBlazor 主色冲突。
- 全站无统一主题、无暗色模式；字体是模板的 Helvetica Neue。

探查确认：bootstrap 的 `row`/`col-`/`navbar`/`nav-link`/`btn-primary` 只出现在 `NavMenu` + `MainLayout` 两个骨架文件，其余页面用的 `d-flex`/`pa-3`/`gap-3` 等都是 MudBlazor 自身工具类。即骨架改 MudBlazor 原生后可干净摘掉 bootstrap。

## 决策

1. **全局交互渲染模式**：在 `App.razor` 给 `<Routes>` 加 `@rendermode="RenderMode.InteractiveServer"`，使整个路由树（含 `MainLayout`）进入交互电路。这是 ADR-0005 所列「安全上提 Provider」的前提。
2. **骨架 MudBlazor 化**：`MainLayout` 改用 `MudLayout` + `MudAppBar` + `MudNavigationMenu` + `MudMainContent`；`NavMenu` 改用 `MudNavLink` + `MudNavGroup` 原生导航。菜单顺序沿用用户设定：首页 → 账号管理 → 今日任务 → 计划任务 → 任务配置（10 子项）→ 管理账户。
3. **四件套上提（作废 ADR-0005）**：`MudThemeProvider` / `MudPopoverProvider` / `MudDialogProvider` / `MudSnackbarProvider` 只在 `MainLayout` 声明一次，删除全部页面内的重复声明。
4. **自定义主题**：主色深空蓝 `#1976D2`，定义亮/暗双 `Palette`；支持暗色模式，顶栏切换按钮，选择持久化到 `localStorage`（key：`bilitool-dark`）。
5. **移除 bootstrap**：`App.razor` 去掉 `bootstrap/bootstrap.min.css` 引用；`app.css` 删除模板残留色，仅保留错误边界与表单校验样式。

## 为什么现在可以安全上提（ADR-0005 的顾虑已消除）

ADR-0005 的失效条件是「面板迁到全局交互或 `MudLayout` 体系」。本 ADR 第 1 条正是把路由树设为全局 `InteractiveServer`，于是 `MainLayout` 落入交互电路，`MainLayout` 里实例化的 Provider 与页面交互电路共享同一套 scoped 服务（`ISnackbar` / `IDialogService` / `IPopoverService`），Snackbar / Dialog / Popover 不再静默失效。原 ADR 的备选方案 A（上提到 App.razor）与本次方案等价，只是落地位置改为 `MainLayout`（因 `MainLayout` 已是交互树的一部分，且天然包裹 `@Body`）。

关于 ADR-0005 提到的 `Client` 项目 `Counter.razor`（`InteractiveAuto`）：页面级 `@rendermode` 会覆盖全局设置，该示例页仍走 `InteractiveAuto`，不冲突；它不是面板功能页，不影响回归。

## 后果

- 全站视觉统一为 MudBlazor 体系，骨架与页面同源；可切换暗色。
- 新增页面**不再需要**抄写四件套 Provider（之前是静默失败的高危抄写约定）；但 Light/Dark 双 palette 与暗色切换逻辑集中在 `MainLayout`。
- 移除 bootstrap 后，任何仍引用 `row`/`col-`/`btn-primary` 的页面会丢失样式——已探查确认除骨架外无页面依赖，移除安全。
- 全局 `InteractiveServer` 使首屏先静态预渲染再增强，主题切换在 `OnAfterRenderAsync` 中读取 `localStorage`，初次可能有极轻微主题闪烁，可接受。

## 备选方案

**A. 仅统一主题与配色，保留 bootstrap 骨架。** 低风险但不彻底，骨架仍是模板风，与页面脱节问题未解决。未采纳。

**B. 页面级精修，骨架不动。** 最保守，但「风格断层」根源未触及。未采纳。

**C. 主题/Provider 留在每页（维持 ADR-0005）。** 与「全站 MudBlazor 化」目标相悖，且多份 Provider 仍是维护负担。未采纳。
