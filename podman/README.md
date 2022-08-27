# Podman 使用说明
<!-- TOC depthFrom:2 -->

- [1. 前期工作](#1-前期工作)
    - [1.1. Podman环境](#11-podman环境)
- [3. 运行容器](#3-运行容器)
- [5. 其他](#5-其他)

<!-- /TOC -->
## 1. 前期工作

### 1.1. Podman环境

请确认已安装了Podman所需环境（[Docker](https://podman.io/)

Podman可以和Docker共存。

安装完成后，请执行`podman -v`检查是否安装成功，请执行`podman info`检查虚拟机环境是否正常。

常用命令参考：

```
# 查看版本
podman -v

# 初始化虚拟机
podman machine init

# 启动虚拟机
podman machine start

# 查看信息
podman info
```

## 3. 运行容器

```
# 生成并运行容器
podman run -itd --name="bilibili_tool_pro" \
    -e Ray_BiliBiliCookies__1="cookie" \
    -e Ray_DailyTaskConfig__Cron="0 15 * * *" \
    -e Ray_LiveLotteryTaskConfig__Cron="0 22 * * *" \
    -e Ray_UnfollowBatchedTaskConfig__Cron="0 6 1 * *" \
    -e Ray_VipBigPointConfig__Cron="7 1 * * *" \
    docker.io/zai7lou/bilibili_tool_pro

# 查看实时日志
podman logs -f bilibili_tool_pro
```

其中，`cookie`需要替换为自己真实的cookie字符串

其他指令参考：

```
# 查看容器运行状态
podman ps -a

# 进入容器
podman exec -it bilibili_tool_pro bash
```

## 5. 其他

镜像使用的仍然是docker仓库的镜像。
