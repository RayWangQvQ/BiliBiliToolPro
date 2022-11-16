#!/usr/bin/env bash
echo -e "\n-------set up dot net env-------"

## 安装dotnet

DOWNLOAD_X64=https://download.visualstudio.microsoft.com/download/pr/b5e6c72e-457c-406c-a4a8-6a4fcdd5f50c/feaa81a666c8942064453fb3c456856b/dotnet-sdk-6.0.403-linux-musl-x64.tar.gz
DOWNLOAD_ARM32=https://download.visualstudio.microsoft.com/download/pr/035f9d69-66ab-4085-b821-9610b464f222/1c2ae5abba2d11dc77b2beece41c5100/dotnet-sdk-6.0.403-linux-musl-arm.tar.gz
DOWNLOAD_ARM64=https://download.visualstudio.microsoft.com/download/pr/8019613a-7cff-4169-a996-1d11786cd97f/62604cf6ee5171e5d97755567e9645d9/dotnet-sdk-6.0.403-linux-musl-arm64.tar.gz

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
    rm -f /usr/local/bin/dotnet
    ln -s ~/.dotnet/dotnet /usr/local/bin
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

dotnet --info

echo -e "\n-------set up dot net env finish-------"