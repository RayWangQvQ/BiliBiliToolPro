# Docker 使用说明
<!-- TOC depthFrom:2 -->

- [1. 前期工作](#1-前期工作)
    - [1.1. Docker环境](#11-docker环境)
    - [1.2. 须知](#12-须知)
- [2. 方式一：Docker Compose(推荐)](#2-方式一docker-compose推荐)
    - [2.1. 启动](#21-启动)
    - [2.2. 修改bili下的docker-compose.yml，填入cookie](#22-修改bili下的docker-composeyml填入cookie)
    - [2.3. 其他命令参考](#23-其他命令参考)
- [3. 方式二：Docker指令](#3-方式二docker指令)
    - [3.1. Docker启动](#31-docker启动)
    - [3.2. 其他指令参考](#32-其他指令参考)
    - [3.3. 使用Watchtower更新容器](#33-使用watchtower更新容器)
- [4. 自己构建镜像（非必须）](#4-自己构建镜像非必须)
- [5. 其他](#5-其他)

<!-- /TOC -->
## 1. 前期工作

### 1.1. Docker环境

请确认已安装了Docker所需环境（[Docker](https://docs.docker.com/get-docker/)和[Docker Compose](https://docs.docker.com/compose/cli-command/)）

Linux一键安装命令:
`curl -fsSL https://get.docker.com | bash -s docker --mirror Aliyun`

Window系统推荐使用Docker Desktop，官方下载安装包安装。

安装完成后，请执行`docker --version`检查`Docker`是否安装成功，请执行`docker compose version`检查`Docker Compose`是否安装成功。

### 1.2. 须知

- Docker有两种部署方式：使用`Docker Compose`或使用docker指令，选择其中一种即可

- 以下章节，凡设计到下载GitHub文件的，如`wget https://raw.githubusercontent.com...`，需要有良好的互联网环境，如果是“局域网”，可以在地址前添加`https://ghproxy.com/`，比如更改为`wget https://ghproxy.com/https://raw.githubusercontent.com...`

- 每次容器启动会去跑一遍 Test 任务，用于测试 Cookie ，其他任务由设定的Cron来指定定时触发。

- 想手动运行某任务的话，[查看功能任务参数](https://github.com/RayWangQvQ/BiliBiliToolPro/tree/develop#2-功能任务说明) 请进入容器后输入命令来启动执行。

## 2. 方式一：Docker Compose(推荐) 

### 2.1. 启动

```
# 创建目录
mkdir bili
cd bili

# 下载
wget https://raw.githubusercontent.com/RayWangQvQ/BiliBiliToolPro/main/src/Ray.BiliBiliTool.Console/appsettings.json
wget https://raw.githubusercontent.com/RayWangQvQ/BiliBiliToolPro/main/docker/sample/docker-compose.yml

# 启动
docker compose up -d

# 查看启动日志
docker logs -f bili
```

### 2.2. 修改bili下的docker-compose.yml，填入cookie

根据 docker-compose.yaml 里面的注释编辑所需配置，`environment` 下可以通过环境变量自由添加自定义配置，其中Cookie是必填的，所以请至少填入Cookie并保存。

保存后，重新运行下`docker compose up -d`

最终文件结构如下：

```
bili
├── appsettings.json
└── docker-compose.yml
```

### 2.3. 其他命令参考

```
# 启动 docker-compose
docker compose up -d

# 停止 docker-compose
docker compose stop

# 查看实时日志
docker logs -f bili

# 进入容器
docker exec -it bili /bin/bash

# 手动更新容器
docker compose pull && docker compose up -d
```

## 3. 方式二：Docker指令

### 3.1. Docker启动

```
# 生成并运行容器
docker run -d --name="bili" \
    -v /bili/Logs:/app/Logs \
    -e Ray_BiliBiliCookies__1="cookie" \
    -e Ray_DailyTaskConfig__Cron="0 15 * * *" \
    -e Ray_LiveLotteryTaskConfig__Cron="0 22 * * *" \
    -e Ray_UnfollowBatchedTaskConfig__Cron="0 6 1 * *" \
    -e Ray_VipBigPointConfig__Cron="7 1 * * *" \
    zai7lou/bilibili_tool_pro

# 查看实时日志
docker logs -f bili
```

其中，`cookie`需要替换为自己真实的cookie字符串

### 3.2. 其他指令参考

```
# 启动容器
docker start bili

# 停止容器
docker stop bili

# 删除容器
docker rm bili

# 进入容器
docker exec -it bili /bin/bash
```

### 3.3. 使用Watchtower更新容器
```
docker run --rm \
    -v /var/run/docker.sock:/var/run/docker.sock \
    containrrr/watchtower \
    --run-once --cleanup \
    bili
```

## 4. 自己构建镜像（非必须）

目前我提供和维护的镜像：`[zai7lou/bilibili_tool_pro](https://hub.docker.com/repository/docker/zai7lou/bilibili_tool_pro)`;

如果有需要（大部分都不需要），可以使用源码自己构建镜像，如下：

在有项目的Dockerfile的目录运行

`docker build -t TARGET_NAME .`

 `TARGET_NAME`为镜像名称和版本，可以自己起个名字

## 5. 其他

代码编译和发布环境: mcr.microsoft.com/dotnet/sdk:6.0

代码运行环境: mcr.microsoft.com/dotnet/runtime:6.0

apt-get 包源用的国内网易的。
