# Docker 使用说明
<!-- TOC depthFrom:2 -->

- [1. 前期工作](#1-前期工作)
    - [1.1. docker环境](#11-docker环境)
    - [1.2. 良好的网络](#12-良好的网络)
- [2. 构建镜像（非必须）](#2-构建镜像非必须)
- [3. 生成容器并运行](#3-生成容器并运行)
    - [3.1. 本地创建文件](#31-本地创建文件)
    - [3.2. 编辑文件内容，填入相关配置](#32-编辑文件内容填入相关配置)
    - [3.3. 启动并运行容器](#33-启动并运行容器)
- [4. 其他](#4-其他)

<!-- /TOC -->
## 1. 前期工作

### 1.1. docker环境

请确认已安装了docker所需环境（docker和docker-compose）。

Window系统推荐使用Docker Desktop，官方下载安装包，一路鼠标点下去就能装好，运行时也有可视化页面。

### 1.2. 良好的网络

第一次运行，可能时间比较长，因为本地没有镜像缓存，第二次之后构建速度就很快了。

## 2. 构建镜像（非必须）

如果不明白什么是镜像，请跳过该步骤去下一节，直接使用我提供和维护的镜像即可：`[zai7lou/bilibili_tool](https://hub.docker.com/repository/docker/zai7lou/bilibili_tool)`;

如果需要自己定制功能，可以使用源码自己构建镜像，如下：

在有Dockerfile的目录运行

`docker build -t TARGET_NAME .`

 `TARGET_NAME`为镜像名称和版本，可以自己起个名字

## 3. 生成容器并运行

推荐使用 docker-compose 来运行容器，步骤如下：

### 3.1. 本地创建文件
在本地任意文件夹下，创建一个目录 `bilibli_tool` ,在其下新建`docker-compose.yml`文件和`my_crontab`文件，文件结构如下：

```
bilibli_tool
├── docker-compose.yml
└── my_crontab
```

### 3.2. 编辑文件内容，填入相关配置
`docker-compose.yml`的文件内容请拷贝 [默认docker-compose.yml](../docker/sample/docker-compose.yml) 内容。其中 `image`（镜像名称）默认是我的，如果要使用自己创建的，请更换为自己创建的镜像名称。`environment` 下可以通过环境变量自由添加自定义配置，其中Cookie是必填的，所以请至少填入Cookie并保存。

`my_crontab`的文件内容请拷贝 [默认my_crontab](../docker/sample/my_crontab) 内容，注意末尾有个换行不要丢了。

### 3.3. 启动并运行容器
在当前目录（有docker-compose.yml的目录）执行启动容器命令：`docker-compose up -d`。

windows 可以使用 cmd 或 powershell ，linux 应该早习惯命令模式了，就不用多说了。

提示成功的话，即表示容器启动成功。

可以进入容器查看详细运行日志，Desktop 通过点击相应容器名就能进入，可以很清晰的看到日志和容器信息；linux 的话请使用相应的docker命令查看，另外当前目录也会生成 Logs 日志文件。

每次容器启动会去跑一遍 Test 任务，用于测试 Cookie 。其他任务由设定的cron来指定定时触发，如需修改定时运行时间，请修改`my_crontab`中的cron表达式，然后再次执行启动容器命令。

想手动运行某任务的话，请进入容器后输入相应命令来启动执行。

## 4. 其他

代码编译和发布环境: mcr.microsoft.com/dotnet/sdk:5.0

代码运行环境: mcr.microsoft.com/dotnet/runtime:5.0

apt-get 包源用的国内网易的
