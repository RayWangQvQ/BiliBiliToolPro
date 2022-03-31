#!/usr/bin/env bash
echo -e "\n-------set up dot net env-------"

DOWNLOAD_X64=https://download.visualstudio.microsoft.com/download/pr/70fb6022-ef53-473b-bfde-dc8cd6b673ca/2c04303064ed5c5158998c3a0d11fcc1/dotnet-sdk-6.0.201-linux-musl-x64.tar.gz
DOWNLOAD_ARM32=https://download.visualstudio.microsoft.com/download/pr/09df51a1-5ef7-4db6-90cd-302ae92b7c84/3d000f08ab919f43f61184a3c48b46a8/dotnet-sdk-6.0.201-linux-musl-arm.tar.gz
DOWNLOAD_ARM64=https://download.visualstudio.microsoft.com/download/pr/0038906f-0d85-41ad-897d-2579359eeb77/78bb1d3b9df9d8017222f0bed5df23ab/dotnet-sdk-6.0.201-linux-musl-arm64.tar.gz

get_download_url_by_machine_architecture() {
    if command -v uname > /dev/null; then
        CPUName=$(uname -m)
        case $CPUName in
        armv*l)
            echo $DOWNLOAD_ARM32
            return 0
            ;;
        aarch64|arm64)
            echo $DOWNLOAD_ARM64
            return 0
            ;;
        esac
    fi
    # Always default to 'x64'
    echo $DOWNLOAD_X64
    return 0
}

DOWNLOAD_URL="$(get_download_url_by_machine_architecture)"
DOTNET_FILE=dotnet-sdk.tar.gz

apk add bash icu-libs krb5-libs libgcc libintl libssl1.1 libstdc++ zlib

wget -O $DOTNET_FILE $DOWNLOAD_URL
DOTNET_ROOT=/home/dotnet
mkdir -p "$DOTNET_ROOT" && tar zxf "$DOTNET_FILE" -C "$DOTNET_ROOT"

export PATH=$PATH:$DOTNET_ROOT
ln -s /home/dotnet/dotnet /usr/local/bin

dotnet --version

echo -e "\n-------set up dot net env finish-------"