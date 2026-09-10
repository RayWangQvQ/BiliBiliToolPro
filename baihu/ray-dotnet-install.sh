#!/usr/bin/env bash
echo -e "\n-------set up dot net env-------"

## 安装dotnet

# 安装系统依赖
install_dependency() {
    echo "安装 Debian 系统依赖..."
    if command -v apt-get >/dev/null 2>&1; then
        apt-get update && apt-get install -y bash curl jq unzip libicu-dev libkrb5-dev libssl-dev zlib1g-dev
    fi
}

install_by_mise() {
    echo "使用 mise 安装 dotnet@8..."
    if command -v mise >/dev/null 2>&1; then
        mise install dotnet@8
    else
        echo "未检测到 mise，请确认当前环境支持 mise"
        exit 1
    fi
}

dotnet() {
    mise exec dotnet@8 -- dotnet "$@"
}

install_dependency

install_by_mise

dotnet --info

echo -e "\n-------set up dot net env finish-------"