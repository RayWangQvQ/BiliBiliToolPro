#!/usr/bin/env bash
echo -e "\n-------set up dot net env-------"

## 安装dotnet

# 安装依赖
install_dependency() {
    echo "安装依赖..."
    apk add bash icu-libs krb5-libs libgcc libintl libssl1.1 libstdc++ zlib
}

# 通过官方脚本安装dotnet
install_by_offical() {
    echo "install by offical script..."
    curl -sSL https://dot.net/v1/dotnet-install.sh | bash /dev/stdin --channel 6.0 --no-cdn --verbose
}

# 创建软链接
create_soft_link() {
    # echo "创建软链接..."
    # rm -f /usr/bin/dotnet
    # ln -s ~/.dotnet/dotnet /usr/bin/dotnet

    echo "添加PATH"
    local exportFile="/root/.bashrc"
    touch $exportFile
    echo '' >> $exportFile
    echo 'export DOTNET_ROOT=$HOME/.dotnet' >> $exportFile
    echo 'export PATH=$PATH:$DOTNET_ROOT:$DOTNET_ROOT/tools' >> $exportFile
    . $exportFile
}

args=("$@")

install_dependency

install_by_offical

create_soft_link

dotnet --info

echo -e "\n-------set up dot net env finish-------"