# ADR-0007：面板窄屏改用响应式抽屉，侧栏宽度必须带 CSS 单位

- 状态：已接受
- 日期：2026-09-28
- 关联：`src/Ray.BiliBiliTool.Web/Components/Layout/MainLayout.razor`；延续 [ADR-0006](./0006-web-ui-mudblazor-native-theme.md)

## 背景

`MainLayout.razor` 里的侧栏原本写作：

```razor
<MudDrawer Open="true" Variant="DrawerVariant.Persistent" Anchor="Anchor.Start" Elevation="1" Width="280">
```

两个各自独立的问题叠加，表现为「改变浏览器长宽后菜单栏盖住内容」：

1. **宽度少了 CSS 单位。** `MudDrawer.Width` 是 `string?`，MudBlazor 把它的值**原样**写进 CSS 自定义属性：`MudDrawerContainer` 输出内联样式 `--mud-drawer-width-left: 280`。这不是合法长度值，于是：
   - `.mud-main-content { margin-left: var(--mud-drawer-width-left) }` 在计算值阶段失效，回退成 `margin-left: 0`；
   - 顶栏的 `margin-left: var(--…); width: calc(100% - var(--…))` 同样失效，顶栏退化为全宽；
   - 抽屉自身 `width: var(--mud-drawer-width, var(--mud-drawer-width-left))` 失效 → `width: auto`，收缩到内容宽（对截图实测：视口 1168 CSS px 时右边缘在 156 px，而非声明的 280）。
   侧栏因此成了「固定在左边缘、却不推内容」的覆盖层，主内容从 x=0 排版、被压在下面。
2. **`Persistent` 抽屉本身不响应断点。** MudBlazor 的 `Breakpoint` 只对 `Responsive` / `Mini` 生效（`MudDrawer.NotifyBrowserViewportChangeAsync` 对 Persistent 直接返回）。即便宽度修对，窄屏下 280px 侧栏仍会挤掉内容；而 `Open="true"` 是硬编码字面量（未绑定 `OpenChanged`），断点自动开合会被参数值压回去，页面上也没有任何汉堡按钮可供收起。

## 决策

1. **修宽度单位**：`Width="280px"`。凡交给 MudBlazor 写进 CSS 变量的尺寸参数都必须自带单位。
2. **抽屉改 `DrawerVariant.Responsive` + `Breakpoint="Breakpoint.Md"`（960px）**，并用 `@bind-Open` 绑定状态（不再用 `Open` 字面量）。≥960px 常驻并推开内容；<960px 变为带遮罩的覆盖式抽屉。
3. **`ClipMode="DrawerClipMode.Always"`**：顶栏保持全宽、品牌留在左上角，抽屉从顶栏下方起。默认的 `Never` 会让顶栏右移 280px、抽屉贯通满高，与面板既有观感不符。
4. **顶栏改 `Fixed="true"`**：`ClipMode.Always` 下抽屉的 `top` 依赖 `--mud-appbar-height`，顶栏若随页面滚走，侧栏上方会露出缝隙；固定后汉堡按钮也不会滚出视野。
5. **汉堡按钮只在 <960px 渲染**（`Class="d-flex d-md-none"`；MudBlazor 的 `.d-md-none` 位于 `@media (min-width: 960px)` 且排在 `.d-flex` 之后，声明顺序保证生效）。未选 `MudHidden`，以免多一个 JS 订阅与首帧闪烁。
6. **覆盖态下切页自动收起**：订阅 `NavigationManager.LocationChanged`，用 `IBrowserViewportService.GetCurrentBreakpointAsync()` 确认当前是 Xs/Sm 再关，避免宽屏上误关常驻侧栏。
7. **删除失效的 scoped CSS**：`MainLayout.razor.css` / `NavMenu.razor.css` 是 Blazor 模板遗留（`.sidebar` / `.top-row` / `.nav-item` / `.bi-*`），当前标记里没有这些类，属纯死代码——排查本次问题时它曾把人带偏。

## 后果

- 窄屏不再遮挡内容；侧栏宽度、内容左偏移、顶栏偏移三者统一由 MudBlazor 的断点 CSS 驱动，无需手写 `@media`。
- 顶栏固定后主内容不再多占 64px 空白：`.mud-main-content` 的 `padding-top: var(--mud-appbar-height)` 是无条件规则，正好抵消固定顶栏所占高度。
- 登录页仍套用同一 `MainLayout`（`Routes.razor` 的 `DefaultLayout` 覆盖所有页面），本次未处理；窄屏下它也会显示汉堡按钮。
- 抽屉开合状态不持久化，每次加载默认展开（窄屏由断点自动收起）。

## 备选方案

**A. 保持 `Persistent`，用 `app.css` 手写 `@media` 覆盖。** 需要自己复刻一份响应式规则，并同步 z-index 与遮罩，等于和库的实现赛跑。未采纳。

**B. `DrawerVariant.Mini` + 断点。** 窄屏收成 56px 图标栏。视觉更连续，但图标栏仍占宽，且要处理 `NavMenu` 的图标/文字切换。未采纳。

**C. 不做响应式，只给页面设 `min-width`。** 窗口过窄时出现横向滚动条而不遮挡。实现最省事，但手机上不可用。未采纳。

**D. `ClipMode` 保持默认 `Never`。** 侧栏贯通满高、品牌漂到侧栏右侧，与既有观感差得远。未采纳。
