#!/usr/bin/env bash
echo -e "\n-------set up dot net env-------"

## 安装dotnet

DOWNLOAD_X64=https://download.visualstudio.microsoft.com/download/pr/d74b9eb9-d60c-4b0d-8d53-f30a6e22b917/ef06d32d3b5206786eac8011798568aa/dotnet-sdk-6.0.405-linux-musl-x64.tar.gz
DOWNLOAD_ARM32=https://download.visualstudio.microsoft.com/download/pr/29682aff-7321-4034-9833-29f3ea435759/cf2fd8a078d3a6c106a1254cc2887ad3/dotnet-sdk-6.0.405-linux-musl-arm.tar.gz
DOWNLOAD_ARM64=https://download.visualstudio.microsoft.com/download/pr/207a3484-7524-4963-9c4e-dacf20ba3a66/4a3bc869dc7a93753022752aa5782989/dotnet-sdk-6.0.405-linux-musl-arm64.tar.gz

get_download_url_by_machine_architecture() {
    if command -v uname >/dev/null; then
        CPUName=$(uname -m)
        case $CPUName in
        armv*l)
            echo $DOWNLOAD_ARM32
            return 0
            ;;
        aarch64 | arm64)
            echo $DOWNLOAD_ARM64
            return 0
            ;;
        esac
    fi
    # Always default to 'x64'
    echo $DOWNLOAD_X64
    return 0
}

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

# 通过vscode域自己下载二进制文件安装dotnet
# https://dotnet.microsoft.com/zh-cn/download/dotnet/6.0
install_by_binaries() {
    echo "install by binaries..."
    DOWNLOAD_URL="$(get_download_url_by_machine_architecture)"
    DOTNET_FILE=dotnet-sdk.tar.gz

    wget -O $DOTNET_FILE $DOWNLOAD_URL
    DOTNET_ROOT=~/.dotnet
    mkdir -p "$DOTNET_ROOT" && tar zxf "$DOTNET_FILE" -C "$DOTNET_ROOT"

    export PATH=$PATH:$DOTNET_ROOT
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

# 不使用官方脚本
no_official=false

while [ $# -ne 0 ]; do
    name="$1"
    case "$name" in
    --no-official | -[Nn]o[Oo]fficial)
        no_official=true
        ;;
    -? | --? | -h | --help | -[Hh]elp)
        script_name="$(basename "$0")"
        echo ".NET Tools Installer"
        echo ""
        echo "$script_name is a simple command line interface for obtaining dotnet cli."
        echo ""
        echo "  --no-official, -NoOfficial                 Do not use the official script, download sdk by visualstudio domain directly."
        echo "  -?,--?,-h,--help,-Help             Shows this help message"
        echo ""
        echo "Install Location:"
        echo "  Location is chosen in following order:"
        echo "    - --install-dir option"
        echo "    - Environmental variable DOTNET_INSTALL_DIR"
        echo "    - $HOME/.dotnet"
        exit 0
        ;;
    *)
        echo "Unknown argument \`$name\`"
        exit 1
        ;;
    esac

    shift
done

install_dependency

if [ "$no_official" = true ]; then
    install_by_binaries
else
    install_by_offical
fi

create_soft_link

dotnet --info

echo -e "\n-------set up dot net env finish-------"