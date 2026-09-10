# 在白虎面板中运行

原理是，利用白虎面板的仓库同步功能，拉取本仓库源码，自动添加 cron 定时任务，并利用白虎内置的 `mise` 自动安装配置 `dotnet` 环境，定时运行相应的 Task。

开始前，请先确保你的白虎面板是运行正常的。

## 1. 步骤

### 1.1. 在白虎面板中添加拉库定时任务

在白虎面板的 **脚本库同步（导入下面的命令）** 页面，填入以下信息：

```
名称：拉取Bili库(不屏蔽dev)
命令：baihu reposync --source-type git --source-url https://github.com/RayWangQvQ/BiliBiliToolPro.git --pre-command "bash baihu/copyshfile.sh"  --blacklist qinglong/DefaultTasks|.git --task-timeout 30 --task-langs '[{"name":"dotnet","version":"8.0"}]'
定时规则：2 3 28 * *
```

```
名称：拉取Bili库(屏蔽dev)
命令：baihu reposync --source-type git --source-url https://github.com/RayWangQvQ/BiliBiliToolPro.git --pre-command "bash baihu/copyshfile.sh"  --blacklist qinglong/DefaultTasks|.git|baihu/DefaultTasks/dev --task-timeout 30 --task-langs '[{"name":"dotnet","version":"8.0"}]'
定时规则：2 3 28 * *
```

保存后，手动运行一次该任务进行仓库拉取。

### 1.2. 检查定时任务

拉库成功后，面板会自动解析 `.sh` 脚本中的注释并在任务列表自动添加 bilibili 相关的 task 定时任务。

### 1.3. 运行环境配置 (自动处理)

白虎面板中，你可以直接运行任务。脚本会自动检测白虎自带的 `mise` 环境管理器，并自动为你极速安装配置所需的 `.NET 8` 环境。一切全自动完成，无需像青龙那样再去手动处理繁琐的依赖问题或下载备用的二进制包。

### 1.4. Bili登录 (支持自动写入)

在白虎面板定时任务中，找到名为 `bili扫码登录` 任务并运行，查看运行日志，扫描日志中的二维码进行登录。

**本项目已适配白虎面板 OpenAPI 自动持久化 Cookie 功能。** 只要您配置了以下环境变量，登录成功后 Cookie 会自动保存/更新到您的白虎面板环境变量中：

1. **白虎 API Token**：
   - 名称：`BaihuConfig__Token`
   - 值：您的白虎面板 API 访问令牌（在白虎面板的【系统设置】->【OPENAI 配置】中生成一个具有openai的key）。
2. **白虎 API 地址 (可选)**：
   - 名称：`BA_URL`
   - 值：您的白虎面板访问地址，例如 `http://localhost:8052`（默认为 `http://localhost:8052`）。

**如果您不配置上述 API 信息，也可以手动处理：**
将日志中成功获取的 Cookie 复制后，在白虎面板的【环境变量】中手动添加：
```
名称：Ray_BiliBiliCookies__0
值：你的Cookie内容
```
（如果有多个账号，依次增加 `Ray_BiliBiliCookies__1`、`Ray_BiliBiliCookies__2` 即可）

## 2. GitHub加速

拉库时，如果服务器在国内，访问GitHub速度慢，可在仓库地址前加上加速代理进行加速。

如：
```
https://gh-proxy.com/https://github.com/RayWangQvQ/BiliBiliToolPro.git
```

## 3. 常见问题

### 3.1. 安装dotnet失败怎么办
如果 `mise` 自动安装失败，通常是网络原因，建议检查主机的网络连通性。如果仍然不行，你可以登录白虎面板，在“编程语言”管理页面中，手动添加并安装 `dotnet@8`。

### 3.2. Couldn't find a valid ICU package installed on the system
如果你使用的是精简版容器遇到类似问题，请在面板环境变量添加如下环境变量：
```
名称：DOTNET_SYSTEM_GLOBALIZATION_INVARIANT
值：1
```
