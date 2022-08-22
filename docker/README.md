# Docker 使用说明
<!-- TOC depthFrom:2 -->

- [1. 前期工作](#1-前期工作)
    - [1.1. Docker环境](#11-docker环境)
	- [1.2. 须知](#12-须知)
- [2. Docker Compose版](#2-docker-compose版推荐)
- [3. Docker版](#3-docker版)
	- [3.1. Docker启动](#31-docker启动)
	- [3.2. 通过Watchtower手动更新容器](#32-通过watchtower手动更新容器)
- [4. 自己构建镜像（非必须）](#4-自己构建镜像非必须)
- [5. 其他](#5-其他)





<!-- /TOC -->
## 1. 前期工作

### 1.1. Docker环境

请确认已安装了Docker所需环境（[Docker](https://docs.docker.com/get-docker/)和[Docker Compose](https://docs.docker.com/compose/cli-command/)）。

Window系统推荐使用Docker Desktop，官方下载安装包，一路鼠标点下去就能装好，运行时也有可视化页面。

安装完成后，请执行`docker --version`检查`docker`是否安装成功，请执行`docker compose version`检查`docker compose`是否安装成功。

### 1.2. 须知


每次容器启动会去跑一遍 Test 任务，用于测试 Cookie 。其他任务由设定的cron来指定定时触发，

想手动运行某任务的话，请进入容器后输入相应命令来启动执行。



## 2. Docker-Compose版(推荐) 
在本地任意文件夹下，创建一个目录 `bilibili_tool` 

```
# 下载项目里面的模板，`my_crontab`文件以及`docker-compose.yml`文件

# 如需修改定时运行时间，请修改`my_crontab`中的cron表达式，然后再次执行启动容器命令。
# 或考虑配置`Ray_Crontab`环境变量
wget https://github.com/RayWangQvQ/BiliBiliToolPro/blob/main/docker/sample/my_crontab

# 根据 docker-compose.yaml 里面的注释编辑所需配置，`environment` 下可以通过环境变量自由添加自定义配置，其中Cookie是必填的，所以请至少填入Cookie并保存。
wget https://github.com/RayWangQvQ/BiliBiliToolPro/blob/main/docker/sample/docker-compose.yml

最终文件结构如下：
bilibili_tool
├── docker-compose.yml
└── my_crontab

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
# 下载项目里面的模板，`my_crontab`文件以及`appsetting.json`文件

# 如需修改定时运行时间，请修改`my_crontab`中的cron表达式，然后再次执行启动容器命令。
wget https://github.com/RayWangQvQ/BiliBiliToolPro/blob/main/docker/sample/my_crontab

# 根据 appsettings.json 里面的注释编辑所需配置
wget https://github.com/RayWangQvQ/BiliBiliToolPro/blob/main/docker/sample/appsettings.json

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

### 3.2. 通过Watchtower手动更新容器
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

在有Dockerfile的目录运行

`docker build -t TARGET_NAME .`

 `TARGET_NAME`为镜像名称和版本，可以自己起个名字

## 5. 其他

代码编译和发布环境: mcr.microsoft.com/dotnet/sdk:6.0

代码运行环境: mcr.microsoft.com/dotnet/runtime:6.0

apt-get 包源用的国内网易的。

