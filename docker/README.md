# Docker 使用说明
<!-- TOC depthFrom:2 -->

- [1. 前期工作](#1-前期工作)
    - [1.1. Docker环境](#11-docker环境)
    - [1.2. 须知](#12-须知)
- [2. Docker-Compose版(推荐)](#2-docker-compose版推荐)
    - [2.1. 启动](#21-启动)
    - [2.2. 修改bili下的docker-compose.yml，填入cookie](#22-修改bili下的docker-composeyml填入cookie)
    - [2.3. 其他命令参考](#23-其他命令参考)
- [3. Docker版](#3-docker版)
    - [3.1. Docker启动](#31-docker启动)
    - [3.2. 使用Watchtower更新容器](#32-使用watchtower更新容器)
- [4. 自己构建镜像（非必须）](#4-自己构建镜像非必须)
- [5. 其他](#5-其他)

<!-- /TOC -->
## 1. 前期工作

### 1.1. Docker环境

请确认已安装了Docker所需环境（[Docker](https://docs.docker.com/get-docker/)和[Docker Compose](https://docs.docker.com/compose/cli-command/)）

Linux一键安装命令:
`curl -fsSL https://get.docker.com | bash -s docker --mirror Aliyun`

Window系统推荐使用Docker Desktop，官方下载安装包，一路鼠标点下去就能装好，运行时也有可视化页面。

安装完成后，请执行`docker --version`检查`Docker`是否安装成功，请执行`docker compose version`检查`Docker Compose`是否安装成功。

### 1.2. 须知


每次容器启动会去跑一遍 Test 任务，用于测试 Cookie ，其他任务由设定的Cron来指定定时触发。

想手动运行某任务的话，[查看功能任务参数](https://github.com/RayWangQvQ/BiliBiliToolPro/tree/develop#2-功能任务说明) 请进入容器后输入命令来启动执行。


## 2. Docker-Compose版(推荐) 

### 2.1. 启动
```
# 创建目录
mkdir bili
cd bili

# 下载
wget https://raw.githubusercontent.com/RayWangQvQ/BiliBiliToolPro/main/src/Ray.BiliBiliTool.Console/appsettings.json
wget https://raw.githubusercontent.com/RayWangQvQ/BiliBiliToolPro/main/docker/sample/docker-compose.yml

# 启动
docker-compose up -d

# 查看启动日志
docker logs -f bilibili_tool_pro
```

### 2.2. 修改bili下的docker-compose.yml，填入cookie

根据 docker-compose.yaml 里面的注释编辑所需配置，`environment` 下可以通过环境变量自由添加自定义配置，其中Cookie是必填的，所以请至少填入Cookie并保存。

保存后，重新运行下`docker-compose up -d`

最终文件结构如下：

bili
├── appsettings.json
└── docker-compose.yml

### 2.3. 其他命令参考

```
# 启动 docker-compose
docker-compose up -d

# 停止 docker-compose
docker-compose stop

# 查看实时日志
docker logs -f bilibili_tool_pro

# 进入容器
docker exec -it bilibili_tool_pro /bin/bash

# 手动更新容器
docker-compose pull && docker-compose up -d
```

## 3. Docker版

### 3.1. Docker启动
```
# 下载项目里面的模板，`my_crontab`文件以及`appsettings.json`文件

# 如需修改定时运行时间，请修改`my_crontab`中的cron表达式，然后再次执行启动容器命令。
wget https://raw.githubusercontent.com/RayWangQvQ/BiliBiliToolPro/main/docker/sample/my_crontab

# 根据 appsettings.json 里面的注释编辑所需配置 （暂定develop分支下载路径）
wget https://raw.githubusercontent.com/RayWangQvQ/BiliBiliToolPro/develop/docker/sample/appsettings.json

# Docker启动命令，/root/bilibili_tool/为映射目录
docker run -d --restart always --name="bilibili_tool_pro" \
    -v /root/bilibili_tool/logs:/app/Logs \
    -v /root/bilibili_tool/my_crontab:/app/custom_crontab \
    -v /root/bilibili_tool/appsettings.json:/app/appsettings.json \
    zai7lou/bilibili_tool_pro

# 查看实时日志
docker logs -f bilibili_tool_pro

# 启动容器
docker start bilibili_tool_pro

# 停止容器
docker stop bilibili_tool_pro

# 删除容器
docker rm bilibili_tool_pro

# 进入容器
docker exec -it bilibili_tool_pro /bin/bash

```

### 3.2. 使用Watchtower更新容器
```
docker run --rm \
    -v /var/run/docker.sock:/var/run/docker.sock \
    containrrr/watchtower \
    --run-once --cleanup \
    bilibili_tool_pro
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
