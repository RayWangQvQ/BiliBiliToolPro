# 在呆呆面板（Daidai Panel）中运行

[呆呆面板](https://github.com/linzixuanzz/daidai-panel) 是一款轻量、现代的定时任务管理面板（Go + Vue + SQLite），定位与青龙类似但更轻量。

本目录提供 BiliBiliToolPro 在呆呆面板中运行的脚本与说明，整体思路与 [`qinglong/`](../qinglong/README.md)、[`baihu/`](../baihu/README.md) 一致：

> 利用面板的「订阅管理」拉取本仓库源码，自动添加 cron 定时任务，然后在面板容器中安装 `dotnet` 环境或下载 `bilitool` 二进制包，定时运行相应的 Task。

## 复用青龙脚本（与各面板保持一致的做法）

各任务脚本（`bili_task_daily.sh` 等）的内容与青龙版**完全一致**——都只是 `. bili_task_base.sh; run_task "Xxx"`，与具体面板无关。所以本目录**不重复维护这些脚本**，而是：

- `daidai/` 只保留一份**面板专属的 `bili_task_base.sh`**（负责呆呆面板下的运行环境安装与定位）；
- 各任务脚本由订阅钩子 [`daidai/copyshfile.sh`](./copyshfile.sh) 在拉库后、建任务前，从 `qinglong/DefaultTasks` 复用拷贝过来；
- `stable` 与 `dev` 共用同一份 base（靠仓库根标记文件 `Ray.BiliBiliTool.sln` 向上定位仓库根目录），进一步减少重复。

这样后续青龙脚本有改动，呆呆面板自动跟随，不需要两边都改。

> ⚠️ 适用范围：与青龙 / 白虎版一样，本方案面向 **Linux 容器形态的呆呆面板**（Docker / Magisk 模块），脚本依赖 `bash`。纯 Windows 单机版不适用。

<!-- TOC -->

- [1. 前置条件](#1-前置条件)
- [2. 步骤](#2-步骤)
  - [2.1. 新建 Open API 应用（拿 AppKey / AppSecret）](#21-新建-open-api-应用拿-appkey--appsecret)
  - [2.2. 配置环境变量](#22-配置环境变量)
  - [2.3. 添加订阅（拉库 + 钩子复用脚本 + 自动建任务）](#23-添加订阅拉库--钩子复用脚本--自动建任务)
  - [2.4. 检查定时任务](#24-检查定时任务)
  - [2.5. 扫码登录](#25-扫码登录)
- [3. 运行模式：dotnet vs bilitool](#3-运行模式dotnet-vs-bilitool)
- [4. 先行版（dev）](#4-先行版dev)
- [5. GitHub 加速](#5-github-加速)
- [6. 常见问题](#6-常见问题)

<!-- /TOC -->

## 1. 前置条件

- 一个正常运行的 **呆呆面板**（推荐 Docker 部署，浏览器能正常打开面板）。
- 面板能访问 GitHub（拉库、下载 dotnet / bilitool 需要）。国内网络见 [第 5 节 GitHub 加速](#5-github-加速)。

呆呆面板默认就支持 `.sh` 脚本订阅、拉库后自动建任务、以及订阅「钩子脚本」，**无需像青龙那样改 `RepoFileExtensions` 配置**。

## 2. 步骤

### 2.1. 新建 Open API 应用（拿 AppKey / AppSecret）

扫码登录成功后，需要有权限把 Cookie 写回面板的环境变量，所以先建一个 Open API 应用用于鉴权。

面板 → **系统设置 → Open API** → 新建应用：

```
名称：bilitool
授权范围（scopes）：envs        # 必须包含 envs（或填 * 全部授权）
速率限制：留空或 0
```

创建后会显示 **AppKey** 和 **AppSecret**（AppSecret 只在创建/重置时完整显示一次，请先复制保存）。

> 如果你不想自动写回 Cookie，可以跳过本步骤。登录任务会在日志里打印出 Cookie，你手动到「环境变量」里添加即可（见 [2.5](#25-扫码登录)）。

### 2.2. 配置环境变量

面板 → **环境变量** → 新建，添加下面两条（变量名照抄，注意是**双下划线** `__`）：

| 变量名 | 值 | 说明 |
| --- | --- | --- |
| `DaiDaiConfig__AppKey` | 上一步的 AppKey | 必填 |
| `DaiDaiConfig__AppSecret` | 上一步的 AppSecret | 必填 |

可选变量：

| 变量名 | 值 | 说明 |
| --- | --- | --- |
| `DaiDai_URL` | `http://127.0.0.1:5700` | 面板地址，**默认就是这个值**，一般不用填。只有改过面板端口、或在别的机器上跑 BiliBiliToolPro 时才需要填成面板实际可达地址（如 `http://192.168.1.10:5700`）。只要协议+主机+端口，不要带路径 |
| `BILI_MODE` | `dotnet` 或 `bilitool` | 运行模式，默认 `dotnet`，见 [第 3 节](#3-运行模式dotnet-vs-bilitool) |
| `BILI_GITHUB_PROXY` | 形如 `https://gh-proxy.com/` | 下载 bilitool 二进制时的 GitHub 加速前缀，仅 `bilitool` 模式用到 |

> 多账号：本工具支持多个 B 站账号，每个账号一条 `Ray_BiliBiliCookies__0`、`Ray_BiliBiliCookies__1`…… 的环境变量。**这些 Cookie 变量由登录任务自动创建/更新，你不用手动建**。

### 2.3. 添加订阅（拉库 + 钩子复用脚本 + 自动建任务）

面板 → **订阅管理** → 新建订阅：

```
名称：Bilibili
类型：Git 仓库（公开仓库）
链接：https://github.com/RayWangQvQ/BiliBiliToolPro.git
分支：develop
定时类型：crontab
定时规则：2 2 28 * *
钩子脚本：bash daidai/copyshfile.sh
白名单：bili_task_
文件后缀：sh
```

> - **钩子脚本填 `bash daidai/copyshfile.sh`**：呆呆面板会在“拉库之后、自动建任务之前”执行它，把青龙目录里的各任务脚本复用到 `daidai/DefaultTasks`，并清理 `qinglong` 目录。这是各任务脚本能复用、不重复维护的关键。
> - **白名单填 `bili_task_`**：只把 `bili_task_*.sh` 登记成定时任务，不会把仓库里其他文件也建成任务。
> - **不要**限制「指定子目录」：钩子需要 `qinglong/` 源、`dotnet` 模式需要编译 `src/` 源码，都要求拉取完整仓库。
> - 没提到的选项保持默认即可（自动添加任务、自动同步默认是开的）。
> - 呆呆面板适配目前在 `develop`（先行版）分支，所以**分支填 `develop`**；等合并进 `main` 后可改回 `main`。

保存后点「运行」拉库。日志里会先看到 `[执行订阅钩子]`（同步脚本），再看到自动扫描 `# cron:` / `# new Env("...")` 并**自动创建 bilibili 定时任务**。

### 2.4. 检查定时任务

面板 → **定时任务**，应能看到自动创建的任务，例如：

- bili每日任务
- bili扫码登录
- bili银瓜子兑换硬币任务
- ……

如果没看到，去订阅的运行日志里看「执行订阅钩子 / 扫描脚本 / 自动添加任务」相关输出排查（一般是网络拉库失败、钩子没填、或白名单写错）。

### 2.5. 扫码登录

在「定时任务」里找到 **bili扫码登录**，点「运行」，查看运行日志，用手机 B 站 App 扫描日志里的二维码登录。

- **首次运行会自动安装运行环境**（dotnet 或 bilitool），时间可能稍长，之后就不用重复装了。
- 登录成功后，如果已按 [2.1](#21-新建-open-api-应用拿-appkey--appsecret) / [2.2](#22-配置环境变量) 配好 Open API，工具会通过面板原生 Open API 自动把 Cookie 写到环境变量 `Ray_BiliBiliCookies__0`（多账号依次 `__1`、`__2`……）。
- 如果没配 Open API，工具会在日志里打印出 `变量Key` 和 `变量值`，**请手动**到面板「环境变量」里照着添加。

之后各个每日任务会按 cron 自动运行，从环境变量读取 Cookie 执行。

## 3. 运行模式：dotnet vs bilitool

通过环境变量 `BILI_MODE` 切换：

| 模式 | 说明 | Cookie 自动写回 |
| --- | --- | --- |
| `dotnet`（默认） | 在面板容器里安装 .NET 8 SDK，直接用 `dotnet run` **从本仓库源码编译运行** | ✅ 立即可用（源码里已包含呆呆面板适配） |
| `bilitool` | 不装 dotnet，直接下载 GitHub 上预编译好的 `bilitool` 二进制运行，更轻量 | ⚠️ 需要等官方发布**包含呆呆面板适配的新版本**后才支持；旧版本二进制不认识 `DaiDai` 平台，登录只会把 Cookie 打印到日志，需手动添加 |

建议：

- 想要**登录自动写回 Cookie 现在就能用** → 用默认的 `dotnet` 模式（拉本仓库源码编译）。
- 面板资源紧张、不想装 dotnet，且能接受首次手动填一次 Cookie（或等新版本二进制） → 用 `bilitool` 模式。

## 4. 先行版（dev）

`develop` 分支的 `bili_dev_task_*.sh` 是开发中的新功能脚本。默认白名单 `bili_task_` 不会匹配它们；如果想体验先行版，把订阅白名单改成：

```
白名单：bili_task_,bili_dev_task_
```

钩子脚本会一并把 `dev/bili_dev_task_*.sh` 复用过来，它们共用同一份 base（`dev/bili_dev_task_base.sh` 只是 source 了上一级的 `bili_task_base.sh`）。

## 5. GitHub 加速

拉库 / 下载慢时，可在订阅链接前加加速代理，例如：

```
https://gh-proxy.com/https://github.com/RayWangQvQ/BiliBiliToolPro.git
```

`bilitool` 模式下载二进制的加速，用环境变量 `BILI_GITHUB_PROXY`（如 `https://gh-proxy.com/`）。加速地址通常不稳定，请自行查找可用的。

## 6. 常见问题

### 6.1. 提示找不到 `bash` / 脚本无法运行

本方案依赖 `bash`，请确保你用的是 **Linux 容器形态的呆呆面板**（Docker / Magisk）。纯 Windows 单机版不适用。

### 6.2. dotnet 安装失败

`dotnet` 模式首次运行会自动装 .NET 8 SDK。如果失败，可：

1. 进面板容器手动按微软官方文档安装 dotnet；或
2. 切换到 `bilitool` 模式（`BILI_MODE=bilitool`，不需要 dotnet）。

### 6.3. Couldn't find a valid ICU package

脚本已默认设置 `DOTNET_SYSTEM_GLOBALIZATION_INVARIANT=1` 规避该问题。如仍报错，可在面板环境变量里再显式加一条：

```
名称：DOTNET_SYSTEM_GLOBALIZATION_INVARIANT
值：1
```

### 6.4. 登录成功但 Cookie 没有自动写回

按以下顺序排查：

1. 是否建了 Open API 应用，且**授权范围包含 `envs`**；
2. 环境变量 `DaiDaiConfig__AppKey` / `DaiDaiConfig__AppSecret` 是否填对（双下划线 `__`）；
3. `DaiDai_URL` 是否能从面板容器内访问到（默认 `http://127.0.0.1:5700`；改过端口或跨机部署要填实际地址）；
4. 用的是不是 `bilitool` 模式且二进制版本过旧（见 [第 3 节](#3-运行模式dotnet-vs-bilitool)）。

登录日志里会打印鉴权与写回的过程，按提示定位即可。失败时工具也会把 Cookie 打印出来，可先手动添加环境变量兜底。

### 6.5. 任务没被自动创建

去订阅的运行日志看：

- 是否有 `[执行订阅钩子]` 且同步了脚本 → 没有就检查「钩子脚本」是否填了 `bash daidai/copyshfile.sh`；
- 「扫描脚本…识别出 N 个含 cron 的脚本」→ 为 0 就检查白名单是否写成了 `bili_task_`、文件后缀是否含 `sh`；
- 一个文件都没扫到 → 多半是拉库失败。
