# 在青龙中运行
总体思路是，在青龙容器中安装 dotnet 环境，然后利用青龙的拉库命令，拉取本仓库源码，添加cron定时任务，定时运行相应的Task。

开始前，请先确保你的青龙面板是运行正常的。

## 第一步：在 `extra.sh` 中添加安装 `dotnet` 环境指令
找到 `extra.sh` 文件，安装青龙面板时一般已经通过 `volumes` 映射到宿主机青龙主目录下的 `data/config` 下，在容器内路径为 `/ql/config/extra.sh`

请将以下内容复制到 `extra.sh` 中：

```
# 安装 dotnet 环境
echo -e "\n-------set up dot net env-------"
apk add bash icu-libs krb5-libs libgcc libintl libssl1.1 libstdc++ zlib
wget https://download.visualstudio.microsoft.com/download/pr/bd94779d-c7c4-47fd-b80a-0088caa0afc6/40f115bbf4c068359e7a066fe0b03dbc/dotnet-sdk-6.0.101-linux-musl-x64.tar.gz
DOTNET_FILE=dotnet-sdk-6.0.101-linux-musl-x64.tar.gz
export DOTNET_ROOT=/home/dotnet
mkdir -p "$DOTNET_ROOT" && tar zxf "$DOTNET_FILE" -C "$DOTNET_ROOT"
export PATH=$PATH:$DOTNET_ROOT
ln -s /home/dotnet/dotnet /usr/local/bin
dotnet --version
echo -e "\n-------set up dot net env finish-------"
```

## 重启青龙容器
重启青龙容器，或在宿主机中执行 `docker exec -it qinglong bash /ql/config/extra.sh`，其中 `qinglong` 是你的容器名。

## 登录青龙面板并添加相应配置
访问青龙面板并登录（通常面板地址为 `ip:5700` ），然后进入`配置文件`页面。

修改 `RepoFileExtensions="js py"` 为 `RepoFileExtensions="js py sh"`

并在末尾添加BiliBili的Cookie作为配置：

```
export Ray_BiliBiliCookies__0="abc"
```

abc为你抓取到的真实cookie字符串。

点击保存按钮，保存配置。

## 在青龙面板中添加拉库定时任务
点击进入`定时任务`页面，点击右上角的`添加任务`按钮，填入以下信息：

```
名称：拉取Bili库
命令：ql repo https://github.com/raywangqvq/bilibilitoolpro.git "bili_task_"
定时规则：8 8 * * 1,3,5
```

点击确定。

保存成功后，找到该定时任务，点击运行按钮，运行拉库。

点击运行按钮傍的日志按钮，可查看运行日志。

如果正常，拉库成功后，同时也会自动添加bilibili相关的task任务。