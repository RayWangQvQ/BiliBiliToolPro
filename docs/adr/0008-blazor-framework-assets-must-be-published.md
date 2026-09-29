# Blazor 框架脚本必须显式随包发布（`RequiresAspNetWebAssets`）

宿主项目 `Ray.BiliBiliTool.Web` 显式声明 `<RequiresAspNetWebAssets>true</RequiresAspNetWebAssets>`，Dockerfile 在 publish 之后断言 `/app/publish/wwwroot/_framework/blazor.web.js` 存在。这两件事一起保证镜像里一定有 Blazor 的引导脚本，不需要依赖「还原时机」这种隐含前提。

## 症状与代价

线上（Docker 部署）表现为：能登录，但登录后几乎所有按钮点了没反应，侧边栏「任务配置」菜单点不开；本地 `dotnet run` 一切正常。根因不是前端代码，而是镜像里**没有** `wwwroot/_framework/blazor.web.js`（`blazor.server.js` 同样缺），浏览器拿不到引导脚本，SignalR 电路（circuit）根本不会建立——所有 `@onclick` 都成了死循环里的空转，而 `<a href>` 跳转和原生表单登录仍能工作，所以故障看起来"只坏了一半"。

诊断证据（v4.1.1-alpha.3 镜像实测）：`/_framework/blazor.webassembly.js`、`/_framework/dotnet.js` 返回 200，`/_framework/blazor.web.js`、`/_framework/blazor.server.js` 返回 404——缺的正好是那两个由 `Microsoft.AspNetCore.App.Internal.Assets` 提供的框架脚本，而 WASM 侧的脚本由 WASM SDK 单独产出，因此没受影响。镜像内 `Ray.BiliBiliTool.Web.staticwebassets.endpoints.json` 只有 555 个终结点，`_framework/blazor.web.js` 的路由完全不存在。

## 为什么会漏

.NET 10 起，`blazor.web.js` / `blazor.server.js` 来自即取即用的 `Microsoft.AspNetCore.App.Internal.Assets` 包，而这个包**是否进入依赖图是还原期决定的**：

```xml
<!-- Microsoft.NET.Sdk.Web.ProjectSystem.targets -->
<Target Name="ResolveRequiredWebAssets" BeforeTargets="ProcessFrameworkReferences">
  <PropertyGroup>
    <RequiresAspNetWebAssets Condition="'$(RequiresAspNetWebAssets)' == '' and @(Content->AnyHaveMetadataValue(Extension, .razor))">true</RequiresAspNetWebAssets>
  </PropertyGroup>
</Target>
```

判定条件是「此刻 `@(Content)` 里有 `.razor` 文件」。而 Dockerfile 为了缓存分层，先只 COPY 各个 `.csproj` 再 `dotnet restore`，**那一刻仓库里一个 `.razor` 都没有**，于是 `RequiresAspNetWebAssets` 为 false，包不下载、`obj/project.assets.json` 里没有它。随后 `COPY . .` 把源码补齐，但 `dotnet publish --no-restore` 复用了那份"没有 .razor 时"的还原结果，框架脚本永远进不了产物。

`--no-restore` 是分水岭：`4.0.8` 镜像里这两个脚本都在，`4.1.0`（#1177 加入 `--no-restore`）起就没了。这也解释了为什么"本地调试是好的"——本地无论 `dotnet run` 还是 `dotnet publish`，还原时源码都在，条件为真。

## 考虑的方案

- **只删 `--no-restore`，让 publish 隐式还原一次**：能修好，但等于把正确性押在"还原时机"上，将来任何一次分层优化都会再次踩雷；也失去 #1177 想要的"publish 不重复还原"。否决。
- **把 `COPY . .` 提到 restore 之前**：正确但丢掉依赖层缓存，源文件一变就全量还原。否决。
- **在 restore 前单独 COPY `Components/**`**：能用，但把一个 MSBuild 内部实现细节写死在 Dockerfile 里，难读也难维护。否决。
- **显式写 `RequiresAspNetWebAssets`（采纳）**：属性写在 `.csproj` 里，任何时机、任何机器上还原都能读到，与 Dockerfile 的分层策略解耦。这也是 ASP.NET Core 团队在 dotnet/aspnetcore#64381 给出的官方开关。
- **加冒烟断言（采纳）**：这类故障的可怕之处是"静默"——构建成功、容器健康、页面能开，只有交互全死。一行 `test -f` 把问题前移到构建期。

## 后果

- `Ray.BiliBiliTool.Web.csproj` 多一个属性，语义是"本项目需要 ASP.NET Core 的框架静态资源"，与是否恰好存在 `.razor` 无关。
- Dockerfile 在 publish 之后多一条断言，镜像构建可能因为缺 `blazor.web.js` 而失败——这是有意为之：宁可构建失败，也不要发一个"能登录但点不动"的镜像。
- 已误发出去的镜像（4.1.0 及之后的 alpha）无法自愈，必须重建；临时救急可以把一个同版本运行时的 `blazor.web.js` 挂进容器 `/app/wwwroot/_framework/`（`UseStaticFiles` 在 `MapStaticAssets` 之前注册，仍会按物理文件提供）。
- 与此前那次 `blazor.web.js` 404（4.0.1.3 补 `app.MapStaticAssets()`）不是同一个 bug：那次缺的是**中间件注册**，这次缺的是**产物里的文件**，两者症状一模一样，排查时不能只看前端表现。
