# Docker 使用说明

### 1

在有Dockerfile的目录运行

`docker build -t TARGET_NAME .`

 `TARGET_NAME`为镜像名称和版本 自己起个名字

### 2

完成后

`docker run -d IMAGE_NAME -v PATH_TO_CONFIGS:/bilibili/config`

`IMAGE_NAME`为刚才镜像的名字

`PATH_TO_CONFIGS`为任意目录 

Window系统推荐使用Docker gui

* 如果目录中含有.json后缀的文件则会以该文件运行BilibiliTool
* 可以放置多个.json文件来实现多账号
* 如果目录为空则会生成一个模板文件方便配置
* 也可以使用`docker volume` 如果没有映射目录则自动生成volume
* 默认每天15点自动运行一次，每次容器启动也会运行一次
* 更改运行时间/频率用`docker exec -it CONTAINER_NAME crontab -e` 默认编辑器是nano

### 3

运行`docker logs CONTAINER_NAME` 查看运行记录

## 

构建环境: mcr.microsoft.com/dotnet/sdk:5.0

运行环境: mcr.microsoft.com/dotnet/aspnet:5.0

大概不支持arm