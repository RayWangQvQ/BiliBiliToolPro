## 添加你需要重启自动执行的任意命令，比如 ql repo
###
 # @Author: your name
 # @Date: 2022-01-26 20:24:25
 # @LastEditTime: 2022-01-26 21:58:47
 # @LastEditors: your name
 # @Description: 打开koroFileHeader查看配置 进行设置: https://github.com/OBKoro1/koro1FileHeader/wiki/%E9%85%8D%E7%BD%AE
 # @FilePath: \BiliBiliToolPro\qinglong\extra.sh
### 
## 安装node依赖使用 pnpm install -g xxx xxx
## 安装python依赖使用 pip3 install xxx

# dotnet
echo -e "\nset up dot net env"
apk add bash icu-libs krb5-libs libgcc libintl libssl1.1 libstdc++ zlib
wget https://download.visualstudio.microsoft.com/download/pr/bd94779d-c7c4-47fd-b80a-0088caa0afc6/40f115bbf4c068359e7a066fe0b03dbc/dotnet-sdk-6.0.101-linux-musl-x64.tar.gz
DOTNET_FILE=dotnet-sdk-6.0.101-linux-musl-x64.tar.gz
export DOTNET_ROOT=/home/dotnet
mkdir -p "$DOTNET_ROOT" && tar zxf "$DOTNET_FILE" -C "$DOTNET_ROOT"
export PATH=$PATH:$DOTNET_ROOT
ln -s /home/dotnet/dotnet /usr/local/bin
dotnet --version
echo "set up dot net env finish"

# 其他代码...
