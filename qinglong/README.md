# 在青龙中运行

总体思路是，在青龙容器中安装 dotnet 环境，利用青龙的拉库命令，拉取本仓库源码，添加cron定时任务，定时运行相应的Task。

开始前，请先确保你的青龙面板是运行正常的。

<!-- TOC depthFrom:2 -->

- [1. 步骤](#1-步骤)
    - [1.1. 安装 `dotnet` 环境](#11-安装-dotnet-环境)
    - [1.2. 重启青龙容器](#12-重启青龙容器)
    - [1.3. 登录青龙面板并修改配置](#13-登录青龙面板并修改配置)
    - [1.4. 添加bili配置](#14-添加bili配置)
    - [1.5. 在青龙面板中添加拉库定时任务](#15-在青龙面板中添加拉库定时任务)
        - [1.5.1 订阅管理](#151-订阅管理)
        - [1.5.2 定时任务拉库](#152-定时任务拉库)
- [2. 先行版](#2-先行版)
    - [2.1 订阅管理](#21-订阅管理)
    - [2.2 定时任务拉库](#22-定时任务拉库)
- [3. GitHub加速](#3-github加速)

<!-- /TOC -->

## 1. 步骤

### 1.1. 安装 `dotnet` 环境
编辑青龙的 `extra.sh` 文件，添加如下指令：

```
# 安装 dotnet 环境
sh -c "$(wget https://ghproxy.com/https://raw.githubusercontent.com/RayWangQvQ/BiliBiliToolPro/main/qinglong/ray-dotnet-install.sh -O -)"
```

![qinglong-extra.png](../docs/imgs/qinglong-extra.png)

### 1.2. 重启青龙容器
重启青龙容器，或在宿主机中执行 `docker exec -it qinglong bash /ql/data/config/extra.sh`，其中 `qinglong` 是你的容器名。

### 1.3. 登录青龙面板并修改配置
青龙面板，`配置文件`页。

修改 `RepoFileExtensions="js py"` 为 `RepoFileExtensions="js py sh"`

保存配置。

### 1.4. 添加bili配置

青龙面板，`环境变量`页，添加环境变量：

```
名称：Ray_BiliBiliCookies__1
值：abc
```

```
名称：Ray_BiliBiliCookies__2
值：defg
```

`abc`、`defg`为你抓取到的真实cookie字符串。

![qinglong-env.png](../docs/imgs/qinglong-env.png)


### 1.5. 在青龙面板中添加拉库定时任务

两种方式：
#### 1.5.1 订阅管理

```
名称：Bilibili
类型：公开仓库
链接：https://github.com/RayWangQvQ/BiliBiliToolPro.git
定时类型：crontab
定时规则：2 2 28 * *
白名单：bili_task_.+\.sh
文件后缀：sh
```

没提到的不要动。

保存后，点击运行按钮，运行拉库。

#### 1.5.2 定时任务拉库
青龙面板，`定时任务`页，右上角`添加任务`，填入以下信息：

```
名称：拉取Bili库
命令：ql repo https://github.com/RayWangQvQ/BiliBiliToolPro.git "bili_task_"
定时规则：2 2 28 * *
```

点击确定。

保存成功后，找到该定时任务，点击运行按钮，运行拉库。

如果正常，拉库成功后，同时也会自动添加bilibili相关的task任务。

![qinglong-tasks.png](../docs/imgs/qinglong-tasks.png)

## 2. 先行版

青龙拉库时可以指定分支，develop分支的代码会超前于默认的main分支，包含当前正在开发的新功能。

想提前体验新功能的朋友可以尝试切换先行版，但同时也意味着稳定性会相应降低（其实是相当于在帮我内测测试bug了~🤨）。

方式有两种：

### 2.1 订阅管理

```
分支：develop
白名单：bili_dev_task_.+\.sh
```

其他同上。

### 2.2 定时任务拉库

修改拉库命令为`ql repo https://github.com/RayWangQvQ/BiliBiliToolPro.git "bili_dev_task_" "" "" "develop"`

## 3. GitHub加速
拉库时，如果服务器在国内，访问GitHub速度慢，可以在仓库地址前加上 `https://ghproxy.com/` 进行加速, 如：`ql repo https://ghproxy.com/https://github.com/RayWangQvQ/BiliBiliToolPro.git "bili_task_"`
