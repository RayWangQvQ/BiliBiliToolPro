# BiliBiliTool

BiliBiliTool是一个针对B站用户自动执行任务的工具，通过它可以实现每日自动观看、分享、投币视频，获取经验，每月自动领取会员权益、自动为自己充电等功能，帮助我们轻松升级会员到Lv6并赚取电池，详细功能目录如下:

* 每天自动登录，获取经验
* 每天自动观看、分享一个视频
* 每天为指定视频投币
* 每天漫画自动签到
* 每天自动直播签到，领取奖励
* 每天自动使用直播中心银瓜子兑换B币，避免浪费
* 每月自动使用快过期的B币券为自己充电
* 每个月自动领取5张B币券和大会员权益

![运行示意](docs/imgs/run-exe.png)

喜欢的话欢迎大家贡献Star~

Github仓库地址：
[RayWangQvQ/BiliBiliTool](https://github.com/RayWangQvQ/BiliBiliTool)

## 1.如何使用

BiliBiliTool实现自动任务的原理，是通过调用一系列B站开放的接口实现的。

举例来说，上面实现观看视频的任务，是通过调用B站的上传视频观看进度接口实现的。

接口Api："https://api.bilibili.com/x/click-interface/web/heartbeat"

入参：视频Id、当前观看时间、用于身份认证的Cookie。

**我们要做的一共有两步，首先是获取自己的Cookie信息并配置到程序中，然后运行BiliBiliTool即可。**

### 1.1.获取自己的Cookie

- 浏览器打开并登录[bilibili网站](https://www.bilibili.com/)
- 按F12打开“开发者工具”，依次点击 应用程序/Application -> 存储-> Cookies
- 找到`DEDEUSERID`、`SESSDATA`、`bili_jct`三项，复制保存它们到记事本，待会儿会用到。

| Name       | Value          |
| ---------- | -------------- |
| DEDEUSERID | 从Cookie中获取 |
| SESSDATA   | 从Cookie中获取 |
| BILI_JCT   | 从Cookie中获取 |

运行BiliBiliTool有两种方式，一种是下载Release包到本地运行，一种是通过Github的Actions实现线上的每天自动运行。

对于想先快速运行调试一下或者没有Github账号的朋友，建议使用第一种方式，操作简单快速。

对于熟悉Github Actions的朋友，推荐使用Github Actions，可以实现线上的每天自动运行，一劳永逸。

### 1.2.运行BiliBiliTool

#### 1.2.1.方式一：本地运行

a. **下载应用文件**

点击[BiliBiliTool/release](https://github.com/RayWangQvQ/BiliBiliTool/releases)，下载已发布的最新版本：

本地如果已经安装了NetCore的环境，推荐下载netcore-dependent.zip文件，因为依赖了本地库（runtime-dependent），所以文件包会很小；

没有环境或不确定有没有的，可以根据操作系统下载对应的zip文件，因为是自包含的（self-contained），文件比较大（而且Github服务器在国外，下载也会比较慢），但是好处是不需要额外安装NetCore的运行时或SDK。

b. **解压并填写配置**

下载并解压后，找到appsettings.json文件，使用记事本编辑，填入之前获取到的Cookie信息，保存后关闭。

![图示](docs/imgs/appsettings-cookie.png)

c. **运行**

找到名称为Ray.BiliBiliTool.Console的可执行文件（Win环境下是Ray.BiliBiliTool.Console.exe），双击运行（Linux使用命令行运行），结果如下：


P.S.如果自己有服务器，也可以将程序发布到自己的服务器，利用自己的任务系统实现每天自动运行。（有服务器的大佬应该就不需要我多BB了）

#### 1.2.2.方式二：Github Actions每天定时线上自动运行
a. **fork本项目**

b. **点击项目 Settings-> Secrets-> New Secrets 添加以下3个Secrets。** 

![Secrets图示](docs/imgs/git-secrets.png)

c. **开启Actions并触发每日自动执行**
   
Github Actions默认处于关闭状态，请手动开启Actions，执行一次工作流，验证是否可以正常工作。

![Actions图示](docs/imgs/run-workflow.png)

Fork仓库后，GitHub默认不自动执行Actions任务，请修改`./github/trigger.json`文件,将`trigger`的值改为`1`，这样每天就会自动执行定时任务了。

```patch
{
- "trigger": 0
+ "trigger": 1
}
```

如果需要修改每日任务执行的时间，请修改`.github/workflows/bilibili-daily-task.yml`:。

```yml
  schedule:
    - cron: '30 6 * * *'
    # cron表达式，Actions时区是UTC时间，比我们东8区要早8个小时，所以如果想每天14点30分运行，则小时数要输入6（14-8=6），如上示例。
```

*如果收到了GitHub Action的错误邮件，请检查Cookies是不是失效了，用户主动清除浏览器缓存，会导致`BILI_JCT`和`DEDEUSERID`失效*

## 2. 个性化自定义配置

待整理

## 3.更新计划

见源码中的todo任务列表。


## 4.感谢
本程序的灵感来源于Github的开源项目：[JunzhouLiu/BILIBILI-HELPER](https://github.com/JunzhouLiu/BILIBILI-HELPER)，该项目由Java编写，有使用Java而不是C#的朋友也可以去Star该项目，他的作者是个很棒的开源分享者，欢迎大家前往支持。

## 5.贡献代码
目前软件还在迭代开发中，如果你有好的想法，欢迎向仓库贡献你的代码，贡献步骤：

a. 搜索查看issue，确定是否已有人提过同类问题

b. 确认没有同类issue后，自己新建issue，描述问题或建议

c. 如果想自己解决，请fork仓库后，在devlop分支进行编码开发，完成后提交pr，并标注解决的issue编号

我会尽快进行代码审核，谢谢你的贡献。

## 6.支持

待更新