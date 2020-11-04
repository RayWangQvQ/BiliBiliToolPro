
![2233](docs/imgs/2233.jpg)

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

**BiliBiliTool是一个B站自动执行任务的工具，通过它可以实现B站帐号的每日自动观看、分享、投币视频，获取经验，每月自动领取会员权益、自动为自己充电等功能，帮助我们轻松升级会员到Lv6并赚取电池**

详细功能目录如下:

* **每天自动登录，获取经验**
* **每天自动观看、分享、投币视频** *（支持指定想要支持的up主，优先选择配置的up主的视频，不配置则随机选取视频）*
* **每天漫画自动签到**
* **每天自动直播签到，领取奖励** *（直播可以不看，但是奖励不领白不领~）*
* **每天自动使用直播中心银瓜子兑换B币，避免浪费**
* **每月自动使用快过期的B币券为自己充电** *（你懂的~）*
* **每个月自动领取5张B币券和大会员权益** *（既然买了会员就要领取该有的奖励啊~）*

![运行图示](docs/imgs/run-exe.png)

Github仓库地址：
[RayWangQvQ/BiliBiliTool](https://github.com/RayWangQvQ/BiliBiliTool)

**本应用仅用于学习和测试，自觉爱护小破站，请勿滥用！**

## 1.如何使用

BiliBiliTool实现自动任务的原理，是通过调用一系列B站开放的接口实现的。

举例来说，要实现观看视频的任务，只需要通过调用B站的上传视频观看进度Api即可，
接口Api："https://api.bilibili.com/x/click-interface/web/heartbeat"，
入参：视频Id、当前观看时间、用于身份认证的Cookie。

BiliBiliTool就是收集了一系列这样的接口，通过每日自动运行程序，来实现自动领取奖励、完成每日任务等功能的。

**要使用BiliBiliTool，我们只需要做两步，首先是获取自己的Cookie作为配置信息，然后将配置输入BiliBiliTool程序并运行即可。**

### 1.1.第一步：获取自己的Cookie

- 浏览器打开并登录[bilibili网站](https://www.bilibili.com/)
- 按 F12 打开“开发者工具”，依次点击 应用程序/Application -> 存储-> Cookies
- 找到`DEDEUSERID`、`SESSDATA`、`bili_jct`三项，复制保存它们到记事本，待会儿会用到。

![获取Cookie图示](docs/imgs/get-bilibili-web-cookie.jpg)

| CookieName | Value          |
| ---------- | -------------- |
| DEDEUSERID | 从Cookie中获取 |
| SESSDATA   | 从Cookie中获取 |
| BILI_JCT   | 从Cookie中获取 |

### 1.2.第二步：运行BiliBiliTool
运行BiliBiliTool有两种方式，一种是通过Github的Actions实现线上的每天自动运行，一种是本地运行或调试。

对于熟悉Github Actions的朋友，推荐使用方式一 Github Actions，可以实现线上的每天自动运行，不需自己动手，一劳永逸。

对于没有Github账号的、或者想先尝个鲜快速运行一下看看、或者是开发者想要本地调试的朋友，可以跳转到方式二，操作简单快速。

#### 1.2.1.运行方式一（推荐）：Github Actions每天定时线上自动运行
Github Actions 是微软巨硬收购G站之后新增的内置CI/CD方案，其核心就是一个可以运行脚本的小型服务器（2核CPU + 7G RAM + 14 G SSD）。

有了它，我们就可以实现每天线上自动运行我们的应用程序。

a. **首先fork本项目到自己的仓库**

b. **进入自己fork的仓库，点击 Settings-> Secrets-> New Secrets 添加以下3个Secrets。它们将作为应用启动时的命令行参数被传入程序。** 

![Secrets图示](docs/imgs/git-secrets.png)

c. **开启Actions并触发每日自动执行**
   
Github Actions默认处于关闭状态，前面都配置好后，请手动开启Actions，执行一次工作流，验证是否可以正常工作。

![Actions图示](docs/imgs/run-workflow.png)

运行结束后，可查看运行日志：

![Actions日志图示](docs/imgs/github-actions-log-1.png)
![Actions日志图示](docs/imgs/github-actions-log-2.png)

workflow的执行策略默认是每天中午2点10分自动执行一次，主分支有push或pr操作会自动执行一次。想要修改策略详见下面常见问题3.2.Actions修改定时任务的执行时间

*如果执行出现异常，会收到了GitHub Action的错误邮件通知，请检查Cookies是不是失效了或者是否有bug。*

*如果是Cookies失效了，用户主动清除浏览器缓存，会导致`BILI_JCT`和`DEDEUSERID`失效。*


#### 1.2.1.运行方式二：本地运行

如果是DotNet开发者，直接clone源码然后vs打开解决方案，配置Cookie后直接运行调试即可。

对于不是开发者的朋友，可以通过下载Release包在本地运行，步骤如下。

a. **下载应用文件**

点击[BiliBiliTool/release](https://github.com/RayWangQvQ/BiliBiliTool/releases)，下载已发布的最新版本

本地如果已经安装了DotNetCore的环境，推荐下载netcore-dependent.zip文件，因为依赖了本地库（runtime-dependent），所以文件包很小；

没有环境或不确定有没有的，可以根据操作系统下载对应的zip文件（window是win-x86-x64.zip），因为是自包含的（self-contained），文件会大些（Github服务器在国外，下载可能比较慢），但是好处是不需要额外安装NetCore的运行时或SDK。

b. **解压并填写配置**

下载并解压后，找到appsettings.json文件，使用记事本编辑，填入之前获取到的Cookie信息，保存后关闭：

![配置文件图示](docs/imgs/appsettings-cookie.png)

c. **运行**

找到名称为 Ray.BiliBiliTool.Console 的可执行文件（Win环境下是Ray.BiliBiliTool.Console.exe），双击运行（Linux使用命令行运行），结果如下：

![运行图示](docs/imgs/run-exe.png)

*P.S.如果自己有服务器，也可以将程序发布到自己的服务器，利用自己的任务系统实现每天自动运行。（有服务器的大佬应该就不需要我多BB了）*

## 2. 个性化自定义配置

个性化配置的详细信息可以在 [appsettings.json](src/Ray.BiliBiliTool.Console/appsettings.json) 文件中查看，有详细的注释信息。

### 2.1.配置方式
目前支持的个性配置方式有如下3种：

#### 方式一：修改appsettings.json文件

如上1.2.2中所演示。

#### 方式二：本地命令行启动，通过命令设置配置

```
dotnet run -p ./src/Ray.BiliBiliTool.Console -userId=123 -sessData=456 -biliJct=789 -numberOfCoins=5
```

命令行参数与配置键的映射关系可以查看 [Constants.cs](src/Ray.BiliBiliTool.Config/Constants.cs) 中的CommandLineMapper，程序会将命令行参数映射为对应的配置键后注册到系统。

#### 方式三：使用Github Actions，通过添加Secrets

如上1.2.1中所演示，在Github Secrets中添加即可。

除了3个必须配置的Cookie外，其他的配置可以通过名为`OTHERCONFIGS`的Secret Key进行配置，其值为多个命令行参数的拼接，如下图所示：

![Github Secrets Other Configs](docs/imgs/github-secrets-other-configs.png)

### 2.2.优先级
以上3种配置方式，其优先级由低到高依次是：文件 < 命令行 < Actions。

即如果既在配置文件中写入了配置值，又在命令行启动时使用命令行参数指定了配置值，则最后会使用命令行的。

（其实Actions最终还是通过命令行参数实现的，只是通过Github Secrets来将配置传入Actions的命令行中的）

对于使用Github Action的朋友，建议使用Secrets进行配置，因为Fork项目后，不会拷贝源仓库中的Secrets，可自由的在自己的仓库中进行私人配置。当有版本重大更新而需要将源仓库同步PR到自己Fork的仓库时，PR操作会很顺滑，不会影响到已配置的值。

如果是Fork之后，自己改了appsettings.json文件再提交的，那么以后如果需要PR源仓库，则要注意保留自己的修改不要被覆盖。

## 3.常见问题

### 3.1.Actions定时任务没有每天自动运行
fork的仓库actions默认是关闭的，需要对仓库进行1次操作才会触发webhook。
可以通过在页面上点击创建wiki来触发，也可以通过任意一次提交推送代码来触发。

### 3.2.Actions修改定时任务的执行时间
如果需要修改每日任务执行的时间，请修改`.github/workflows/bilibili-daily-task.yml` 中的cron表达式:

```yml
  schedule:
    - cron: '30 6 * * *'
    # cron表达式，Actions时区是UTC时间，比我们东8区要早8个小时，所以如果想每天14点30分运行，则小时数要输入6（14-8=6），如上示例。
```

## 4.更新计划

见源码中的todo任务列表。

## 5.贡献代码
目前软件还在迭代开发中，如果你有好的想法，欢迎向仓库贡献你的代码，贡献步骤：

a. 搜索查看issue，确定是否已有人提过同类问题

b. 确认没有同类issue后，自己可新建issue，描述问题或建议

c. 如果想自己解决，请fork仓库后，在devlop分支进行编码开发，完成后提交pr，并标注解决的issue编号

我会尽快进行代码审核，提前感谢你的贡献。

## 6.捐赠支持

如果觉得我写的程序对你小有帮助

或者，就是单纯的想集资给我买瓶霸王增发液

那么下面的赞赏码可以扫一扫啦

（赞赏时记得留下【昵称】和【留言】~）

微信扫码自动赞赏1元：

![微信赞赏码](docs/imgs/donate-wechat.jpg)

支付宝扫码自动赞赏1元：

![支付宝赞赏码](docs/imgs/donate-ali.jpg)

## 7.感谢
本程序的灵感来源于Github的开源项目：[JunzhouLiu/BILIBILI-HELPER](https://github.com/JunzhouLiu/BILIBILI-HELPER)，该项目由Java编写，有使用Java而不是C#的朋友也可以去Star该项目，他的作者是个很棒的开源分享者，欢迎大家前往支持。
