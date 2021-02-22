![2233](https://cdn.jsdelivr.net/gh/RayWangQvQ/BiliBiliTool.Docs@main/imgs/2233.png)

<div align="center">

<h1 align="center">

BiliBiliTool

</h1>

[![GitHub Stars](https://img.shields.io/github/stars/RayWangQvQ/BiliBiliTool?style=flat-square)](https://github.com/RayWangQvQ/BiliBiliTool/stargazers)
[![GitHub Forks](https://img.shields.io/github/forks/RayWangQvQ/BiliBiliTool?style=flat-square)](https://github.com/RayWangQvQ/BiliBiliTool/network)
[![GitHub Issues](https://img.shields.io/github/issues/RayWangQvQ/BiliBiliTool?style=flat-square)](https://github.com/RayWangQvQ/BiliBiliTool/issues)
[![GitHub Contributors](https://img.shields.io/github/contributors/RayWangQvQ/BiliBiliTool?style=flat-square)](https://github.com/RayWangQvQ/BiliBiliTool/graphs/contributors)
[![GitHub All Releases](https://img.shields.io/github/downloads/RayWangQvQ/BiliBiliTool/total?style=flat-square)](https://github.com/RayWangQvQ/BiliBiliTool/releases)
![GitHub Release (latest SemVer)](https://img.shields.io/github/v/release/RayWangQvQ/BiliBiliTool?style=flat-square)
[![GitHub License](https://img.shields.io/github/license/RayWangQvQ/BiliBiliTool?style=flat-square)](https://github.com/RayWangQvQ/BiliBiliTool/blob/main/LICENSE)

</div>

[目录]

<!-- TOC depthFrom:2 -->

- [1. 如何使用](#1-如何使用)
    - [1.1. 第一步：获取自己的 Cookie](#11-第一步获取自己的-cookie)
    - [1.2. 第二步：配置 Cookie 并运行 BiliBiliTool](#12-第二步配置-cookie-并运行-bilibilitool)
        - [1.2.1. 运行方式一：Github Actions 线上定时自动运行（推荐）](#121-运行方式一github-actions-线上定时自动运行推荐)
        - [1.2.2. 运行方式二：下载程序包到本地或服务器运行](#122-运行方式二下载程序包到本地或服务器运行)
- [2. 个性化自定义配置](#2-个性化自定义配置)
- [3. 常见问题](#3-常见问题)
- [4. 版本发布及更新](#4-版本发布及更新)
- [5. 贡献代码](#5-贡献代码)
- [6. 捐赠支持](#6-捐赠支持)
- [7. API 参考](#7-api-参考)

<!-- /TOC -->

**BiliBiliTool 是一个 B 站自动执行任务的小工具，当我们忘记做 B 站的某项任务时，它会像一个小助手一样，按照我们预先吩咐她的命令，帮助我们自动完成计划的任务。**

比如，当我们忘记领取自己的大会员福利时，她会帮助我们自动领取；当我们忘记完成每日任务时，她会辅助我们自动完成所有任务，获取每日的满额65点经验值，快速升级 Lv6 ；当然我们也可以用她来支持我们喜欢的up主，拒绝白嫖~

详细功能如下：

- **每天自动登录，获取硬币奖励与 5 点经验**
- **每天自动观看一个视频，获取 5 点经验（只会在未完成任务时执行，支持指定想要支持的up主）**
- **每天自动分享一个视频，获取 5 点经验（只会在未完成任务时执行，支持指定想要支持的up主）**
- **每天智能投币，拒绝白嫖，顺便获取满额 50 点经验（只会在未完成任务时执行，不会多投，支持指定投币数量和想要支持的up主）**
- **每天自动漫画签到**
- **每天自动直播签到**
- **直播中心银瓜子自动兑换为硬币**
- **每月自动领取大会员赠送的 5 张 B 币券和福利（忘记或者不领就浪费了哦）**
- **每月自动领取大会员漫画福利**
- **月底在 B 币券过期前为自己充电（支持指定想要支持的up主，如果没有喜欢的up，也可以为自己充个电啊，做个用爱为自己发电的人~）**
- **理论上支持所有远端的日志推送（默认支持推送到Telegram、企业微信、Server酱、钉钉、酷推，另外也支持自定义推送到任意api）**

**另外，通过结合 GitHub Actions，可以实现每天线上自动运行，只要部署一次，小助手就会在背后一直默默地帮我们完成我们预先布置的任务。**

还有其他一些辅助小功能，大家可以自己去自由探索~

![运行图示](https://cdn.jsdelivr.net/gh/RayWangQvQ/BiliBiliTool.Docs@main/imgs/run-exe.png)

**Github 仓库地址：[RayWangQvQ/BiliBiliTool](https://github.com/RayWangQvQ/BiliBiliTool)**

**注意：**

- **本应用仅用于学习和测试，作者本人并不对其负责，请于运行测试完成后自行删除，请勿滥用！**
- **所有代码都是开源且透明的，任何人均可查看，程序不会保存或滥用任何用户个人信息**
- **应用内几乎所有功能都开放为了配置（比如任务开关、日期、upId等），请仔细阅读配置文档，自己对自己的配置负责**

_（如果图片挂了，是因为 GitHub 的服务器在国外，经常会刷不出，有 VPN 的开启 VPN，没有的也可先先参考 [我的博客](https://www.cnblogs.com/RayWang/p/13909784.html)，但博客内容不保证最新)_

## 1. 如何使用

BiliBiliTool 实现自动任务的原理，是通过调用一系列 B 站开放的接口实现的。

BiliBiliTool 就是收集了一系列的 B 站开放api，通过每日自动运行程序，依次调用接口，来实现各定制任务的。

**要使用 BiliBiliTool，我们只需要做两步：首先是获取自己的 Cookie 作为配置信息，然后将其输入 BiliBiliTool 并运行即可。**

### 1.1. 第一步：获取自己的 Cookie

- 浏览器打开并登录 [bilibili 网站](https://www.bilibili.com/)
- 登录成功后，访问 `https://api.bilibili.com/x/web-interface/nav`，按 **F12** 打开"开发者工具"，按 **F5** 刷新一下
- 在"开发者工具"面板中，点击 **网络（Network）**，在左侧的请求列表中，找到名称为 `nav` 的接口，点击它
- 依次查找 **Headers** ——> **RequestHeader** ——> **cookie**，可以看到很长一串以英文分号分隔的字符串，复制整个这个cookie字符串（不要使用右键复制，请使用 Ctrl+C 复制，右键会进行 UrlDecode ），保存它们到记事本，待会儿会用到。

![获取Cookie图示](https://cdn.jsdelivr.net/gh/RayWangQvQ/BiliBiliTool.Docs@main/imgs/get-bilibili-web-cookie.jpg)

### 1.2. 第二步：配置 Cookie 并运行 BiliBiliTool

运行 BiliBiliTool 有两种方式，一是通过 Github 的 Actions 实现线上的每天自动运行，二是通过下载Release包到本地或服务器运行。

对于熟悉 Github 的朋友，推荐使用方式一 Github Actions，可以实现线上的每天自动运行，不需自己动手，一劳永逸。

对于没有 Github 账号的、或者想先快速运行一下尝个鲜、或是要部署到自己服务器的朋友，可以跳转到方式二 Release 包运行，操作简单快速。

#### 1.2.1. 运行方式一：Github Actions 线上定时自动运行（推荐）

Github Actions 是微软（巨硬）收购 G 站之后新增的内置 CI/CD 方案，其核心就是一个可以运行脚本的小型服务器（2 核 CPU + 7G RAM + 14 G SSD）。

有了它，我们就可以实现每天线上自动运行我们的应用程序，通过合理配置还可以实现版本的同步更新。

<details>

Ⅰ. **首先 fork 本项目到自己的仓库**

Ⅱ. **进入自己 fork 的仓库，点击 Settings-> Secrets-> New Secrets， 添加 1 个 Secrets，其名称为`COOKIESTR`，值为刚才我们保存的 `cookie 字符串`。它们将作为配置项，在应用启动时传入程序。**

![Secrets图示](https://cdn.jsdelivr.net/gh/RayWangQvQ/BiliBiliTool.Docs@main/imgs/git-secrets.png)

![添加CookieStr图示](https://cdn.jsdelivr.net/gh/RayWangQvQ/BiliBiliTool.Docs@main/imgs/git-secrets-add-cookie.png)


Ⅲ. **开启 Actions 并触发每日自动执行**

刚 Fork 完，所有 Actions 都是默认关闭的，都配置好后，需要手动点击 Enable 开启 Actions。开启后请手动执行一次工作流，验证是否可以正常工作，操作步骤如下图所示：

![Actions图示](https://cdn.jsdelivr.net/gh/RayWangQvQ/BiliBiliTool.Docs@main/imgs/run-workflow.png)

运行结束后，请查看运行日志：

![Actions日志图示](https://cdn.jsdelivr.net/gh/RayWangQvQ/BiliBiliTool.Docs@main/imgs/github-actions-log-1.png)
![Actions日志图示](https://cdn.jsdelivr.net/gh/RayWangQvQ/BiliBiliTool.Docs@main/imgs/github-actions-log-2.png)

Actions 的执行策略默认是每天 0 点整触发运行，如要设置为指定的运行时间，请详见下面**常见问题**章节中的《**Actions 如何修改定时任务的执行时间？**》

**建议每个人都设置下每日执行时间！不要使用默认时间！最好也不要设定在整点，错开峰值，避免 G 站的同一个IP在相同时间去请求 B 站接口，导致 IP 被禁！**

如果配置了推送，执行成功后接收端会收到推送消息，如下所示为Server酱微信推送效果：

![微信推送图示](https://cdn.jsdelivr.net/gh/RayWangQvQ/BiliBiliTool.Docs@main/imgs/wechat-push.png)

目前默认支持**Telegram推送、企业微信推送、钉钉推送、Server酱推送和酷推QQ推送**，如果需要推送到其他端，也可以配置为任意的可以接受消息的Api地址，关于如何配置推送请详见下面的**个性化自定义配置**章节。

_如果执行出现异常，会收到了 GitHub Action 的错误邮件通知，请检查 Cookies 是不是失效了或者是否有 bug。_

_如果是 Cookies 失效了，请从浏览器重新获取并更新到 Secrets 中。_

_如果是发现 bug，请先确认是否可以通过升级到最新版本解决，然后搜索文档（特别是配置说明文档和常见问题文档）和issues，查看是否已有其他人遇到相同问题、是否已有解决方案，如果还为解决可以提交 issue，我会尽快确认并解决。（如何正确的提交issue，请详见下面**常见问题**章节。_
</details>

#### 1.2.2. 运行方式二：下载程序包到本地或服务器运行

如果是 DotNet 开发者，直接 clone 源码然后 vs 打开解决方案，配置 Cookie 后即可直接本地进行运行和调试。

对于不是开发者的朋友，可以通过下载 Release 包到本地、docker或任意服务器运行，步骤如下。
<details>

Ⅰ. **下载应用文件**

点击 [BiliBiliTool/release](https://github.com/RayWangQvQ/BiliBiliTool/releases)，下载已发布的最新版本。

* 如果本地已安装 `.NET 5.0` 环境：

请下载 `net-dependent.zip` 文件，本文件依赖本地运行库（runtime-dependent），所以文件包非常小（不到1M）。

P.S.这里的运行环境指的是 `ASP.NET Core Runtime 5.0.0`与`.NET Runtime 5.0.0` ，安装方法可详见 [常见问题](https://github.com/RayWangQvQ/BiliBiliTool.Docs/blob/main/questions.md) 中的 **本地或服务器如何安装.net环境**

* 如果不希望安装或不知如何安装.net运行环境：

请根据操作系统下载对应的 zip 文件，此文件已自包含（self-contained）运行环境，但相较不包含运行时的文件略大（20M 左右，Github 服务器在国外，下载可能比较慢）。

如，Windows系统请下载 `win-x86-x64.zip` ，其他以此类推。


Ⅱ. **解压并填写配置**

下载并解压后，找到 appsettings.json 文件，使用记事本编辑，填入之前获取到的 Cookie 字符串，保存后关闭：

![配置文件图示](https://cdn.jsdelivr.net/gh/RayWangQvQ/BiliBiliTool.Docs@main/imgs/appsettings-cookie.png)

Ⅲ. **运行**

* Windows 系统

对于已安装.net环境，且使用的是依赖包，可在当前目录下执行命令：`dotnet Ray.BiliBiliTool.Console.dll`，或者直接双击运行名称为 start.bat 的批处理文件，均可运行。

对于使用自包含运行环境版本的，可直接双击运行名称为 Ray.BiliBiliTool.Console.exe 的可执行文件。

* Linux 系统

对于已安装.net环境，且使用的是依赖包，同上，可在终端中执行命令：`dotnet Ray.BiliBiliTool.Console.dll`

对于使用独立包的，可在终端中执行命令：`Ray.BiliBiliTool.Console`。

其他系统依此类推，运行结果图示如下：

![运行图示](https://cdn.jsdelivr.net/gh/RayWangQvQ/BiliBiliTool.Docs@main/imgs/run-exe.png)

除了修改配置文件，也可以通过添加环境变量或在启动命令后附加参数来实现配置，详细方法可参考下面的**配置说明**章节。

_P.S.如果自己有服务器，也可以将程序发布到自己的服务器，利用自己的任务调度系统实现每天自动运行。（有服务器的大佬应该就不需要我多 BB 了）_
</details>

## 2. 个性化自定义配置

[>>点击查看配置说明文档](https://github.com/RayWangQvQ/BiliBiliTool.Docs/blob/main/configuration.md)

## 3. 常见问题

[>>点击查看常见问题文档](https://hub.fastgit.org/RayWangQvQ/BiliBiliTool.Docs/blob/main/questions.md)

## 4. 版本发布及更新

当前正处于稳定的迭代开发中，正常情况下每 2 周会发布一个小版本，详细待更新内容可参见源码中的 todo 任务列表。

关于版本发布更新后，如何同步最新的内容到自己 Fork 的仓库，可查看**常见问题文档**中的 《**我 Fork 之后如何同步原作者的更新内容？**》章节。

## 5. 贡献代码

如果你有好的想法，欢迎向仓库贡献你的代码，贡献步骤：

* 搜索查看 issue，确定是否已有人提过同类问题

* 确认没有同类 issue 后，自己可新建 issue，描述问题或建议

* 如果想自己解决，请 fork 仓库后，在**devlop 分支**进行编码开发，完成后**提交 pr 到 devlop 分支**，并标注解决的 issue 编号

我会尽快进行代码审核，提前感谢您的贡献。

## 6. 捐赠支持

[>>点击查看已捐赠列表和留言](https://hub.fastgit.org/RayWangQvQ/BiliBiliTool.Docs/blob/main/donate-list.md)

个人维护开源不易

如果觉得我写的程序对你小有帮助

或者，就是单纯的想集资给我买瓶霸王增发液

那么下面的赞赏码可以扫一扫啦

（赞赏时记得留下【昵称】和【留言】~ 另外我发现很多留言想要进群或者加好友的，一定一定要记得留下微信号哈，微信赞赏页面是看不到微信号的）

扫码赞赏：

![赞赏码](https://cdn.jsdelivr.net/gh/RayWangQvQ/BiliBiliTool.Docs@main/imgs/donate.jpg)

## 7. API 参考

- [SocialSisterYi/bilibili-API-collect](https://github.com/SocialSisterYi/bilibili-API-collect)

- [JunzhouLiu/BILIBILI-HELPER](https://github.com/JunzhouLiu/BILIBILI-HELPER)

- [happy888888/BiliExp](https://github.com/happy888888/BiliExp)
