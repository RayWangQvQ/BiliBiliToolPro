# 应用版本：以程序集 InformationalVersion 为唯一载体，非 CI 产物折叠为兜底文案

面板要展示「应用版本」。版本号在构建期由 CI 算出（ADR-0002），运行时需要一个载体把它读出来。选定：载体就是宿主程序集的 `AssemblyInformationalVersion`（由 `dotnet publish -p:Version=` 烘焙），运行时统一经 `Ray.BiliBiliTool.Config.AppVersion` 读取；读不到版本元数据、或版本串为本地构建兜底值 `0.0.0-dev` 时，界面与日志一律显示 `开发版`，不暴露占位串。

## 考虑的方案

- **环境变量（Dockerfile 加 `ENV VERSION`）**：被否决。两条发布路径不对等——Docker 镜像走 `Dockerfile`，release zip 走 `scripts/publish.sh` 打 Console，env 只能覆盖前者，会让"镜像里的版本"和"zip 里的版本"有各自的真相源。且编排层（青龙、K8s）传 env 全靠人工，漏传就静默降级。
- **构建期生成 `version.txt` 落产物目录**：被否决。多一个需要与程序集版本保持同步的文件；容器只读文件系统、青龙等把可执行体拷来拷去的场景都会把它弄丢，丢了的后果是"版本显示为空"这种静默错误。
- **冗余多载体（程序集 + env + 文件，取优先级最高的）**：被否决。三个来源迟早不同步，而异步时无法判断哪个是对的；单一载体不一致时至少是"整体错"，可诊断。
- **永远显示原始版本串（`0.0.0-dev` 也照显）**：被否决。本地构建与 CI 产物会显示成同一个形状的版本号，用户报 issue 时无法区分，等于把"这份产物从哪来"这个关键事实藏了起来。

## 后果

- 版本只能由构建期注入，运行时无法覆盖：想改版本号必须重新构建。这是有意为之——版本是产物属性，不是配置。
- 想不启动应用就查版本的运维场景，只能看 Docker 镜像的 `org.opencontainers.image.version` LABEL（已有）或 Console 启动日志，容器文件系统里没有版本文件。
- `-alpha.N` 这类预发布后缀只有 InformationalVersion 装得下，CLR 的数字 `AssemblyVersion` 装不下——所以读取路径固定走它，尾部 `+commit` 对用户无意义故截掉。
- `0.0.0-dev` 这个兜底值同时出现在 `common.props`、`scripts/publish.sh`、`AppVersion.LocalBuildVersion` 三处，改动需同步（`AppVersionTests` 里有一条断言钉住它）。
- Console 与 Web 现在共用 `AppVersion.DisplayOf`，因此 Console 启动日志里版本号的 `v` 前缀被去掉（前缀会和兜底中文文案拼成 `v开发版`）。
