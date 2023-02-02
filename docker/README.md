# Docker 使用说明
<!-- TOC depthFrom:2 -->

- [1. 前期工作](#1-前期工作)
    - [1.1. Docker环境](#11-docker环境)
    - [1.2. 须知](#12-须知)
- [2. 方式一：Docker Compose(推荐)](#2-方式一docker-compose推荐)
    - [2.1. 启动](#21-启动)
    - [2.2. 其他命令参考](#22-其他命令参考)
- [3. 方式二：Docker指令](#3-方式二docker指令)
    - [3.1. Docker启动](#31-docker启动)
    - [3.2. 其他指令参考](#32-其他指令参考)
    - [3.3. 使用Watchtower更新容器](#33-使用watchtower更新容器)
- [4. 登录](#4-登录)
- [5. 自己构建镜像（非必须）](#5-自己构建镜像非必须)
- [6. 其他](#6-其他)

<!-- /TOC -->
## 1. 前期工作

### 1.1. 注意事项

- 你需要有良好的网络环境。如果你在中国大陆，对于Github文件可以使用[GitHub Proxy](https://ghproxy.com)来处理，而对于Docker镜像，请使用[镜像源](https://mirrors.ustc.edu.cn/help/dockerhub.html)

- 每次容器启动会去跑一遍 Test 任务，用于测试 Cookie ，其他任务由设定的Cron来指定定时触发。

- 想手动运行某任务的话，[查看功能任务参数](https://github.com/RayWangQvQ/BiliBiliToolPro/tree/develop#2-功能任务说明) 请进入容器后输入命令来启动执行。

### 1.2. Docker环境

请确认已安装了Docker与Docker Compose所需环境（[Docker](https://docs.docker.com/get-docker) 与 [Docker Compose](https://docs.docker.com/compose/cli-command)）

安装完成后，请执行`docker --version`检查`Docker`是否安装成功，请执行`docker compose version`检查`Docker Compose`是否安装成功。

## 2. 启动与维护

### 2.1. 启动

```
# 创建目录
mkdir bili
cd bili

# 下载
wget https://raw.githubusercontent.com/RayWangQvQ/BiliBiliToolPro/main/docker/sample/appsettings.json
wget https://raw.githubusercontent.com/RayWangQvQ/BiliBiliToolPro/main/docker/sample/cookies.json
wget https://raw.githubusercontent.com/RayWangQvQ/BiliBiliToolPro/main/docker/sample/docker-compose.yml

# 构建&启动
sudo docker compose up -d

# 查看启动日志
sudo docker logs -f bili
```

最终文件结构如下：

```
bili
├── appsettings.json
├── cookies.json
└── docker-compose.yml
```

### 2.2. 维护

```
# 启动 docker-compose
sudo docker compose up -d

# 停止 docker-compose
sudo docker compose stop

# 查看实时日志
sudo docker logs -f bili

# 进入容器
sudo docker exec -it bili /bin/bash

# 手动更新容器
sudo docker compose pull && sudo docker compose up -d
```

## 3. 登录

在宿主机运行`sudo docker exec -it bili bash -c "dotnet Ray.BiliBiliTool.Console.dll --runTasks=Login"`

扫码进行登录。

![login](../docs/imgs/docker-login.png)

## 4. 自己构建镜像（非必须）

目前我提供和维护的镜像：`[zai7lou/bilibili_tool_pro](https://hub.docker.com/repository/docker/zai7lou/bilibili_tool_pro)`;

如果有需要（大部分都不需要），可以使用源码自己构建镜像，如下：

在有项目的Dockerfile的目录运行

`docker build -t TARGET_NAME .`

 `TARGET_NAME`为镜像名称和版本，可以自己起个名字

## 5. 其他

代码编译和发布环境: mcr.microsoft.com/dotnet/sdk:6.0

代码运行环境: mcr.microsoft.com/dotnet/runtime:6.0
