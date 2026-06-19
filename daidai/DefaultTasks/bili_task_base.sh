#!/usr/bin/env bash
# cron:0 0 1 1 *
# new Env("bili_base")

# 呆呆面板（Daidai Panel）基础脚本。
# 设计与 qinglong / baihu 的集成一致：每个面板只维护“自己这一份 base（环境安装部分）”，
# 而 bili_task_daily.sh 等“各任务脚本”完全复用 qinglong 目录里的同名脚本——
# 由订阅钩子 daidai/copyshfile.sh 在拉库后、建任务前从 qinglong/DefaultTasks 拷贝过来。
# 所以本目录默认只放 base，不重复维护各任务脚本（详见 daidai/README.md）。
#
# 本 base 与 qinglong 版的差异只在“面板专属的运行环境安装/定位”：
#   1. 用仓库根标记文件（Ray.BiliBiliTool.sln）向上查找仓库根目录，stable 与 dev 共用同一份 base；
#   2. Ray_PlatformType 设为 DaiDai；登录后通过呆呆面板原生 Open API 写回 Cookie。

# Stop script on NZEC
set -e
# Stop script if unbound variable found (use ${var:-} if intentional)
set -u
# By default cmd1 | cmd2 returns exit code of cmd2 regardless of cmd1 success
set -o pipefail

verbose=false                          # 开启debug日志
bili_repo="raywangqvq/bilibilitoolpro" # 仓库地址（bilitool 模式下载 release 用）
prefer_mode=${BILI_MODE:-"dotnet"}     # dotnet 或 bilitool，可通过面板环境变量 BILI_MODE 配置
github_proxy=${BILI_GITHUB_PROXY:-""}  # 下载 github release 包时使用的代理，拼在地址前面
export DOTNET_SYSTEM_GLOBALIZATION_INVARIANT=1 # 解决 ICU 相关抽风问题

invocation='say_verbose "Calling: ${yellow:-}${FUNCNAME[0]} ${green:-}$*${normal:-}"'

# 暴露 stream 3 作为函数内的“屏幕输出”，避免污染函数返回值
exec 3>&1

if [ -t 1 ] && command -v tput >/dev/null; then
    ncolors=$(tput colors || echo 0)
    if [ -n "$ncolors" ] && [ $ncolors -ge 8 ]; then
        bold="$(tput bold || echo)"
        normal="$(tput sgr0 || echo)"
        red="$(tput setaf 1 || echo)"
        green="$(tput setaf 2 || echo)"
        yellow="$(tput setaf 3 || echo)"
        cyan="$(tput setaf 6 || echo)"
    fi
fi

say_warning() { printf "%b\n" "${yellow:-}bilitool: Warning: $1${normal:-}" >&3; }
say_err() { printf "%b\n" "${red:-}bilitool: Error: $1${normal:-}" >&2; }
say() { printf "%b\n" "${cyan:-}bilitool:${normal:-} $1" >&3; }
say_verbose() { if [ "$verbose" = true ]; then say "$1"; fi; }

# 尝试加载已安装 dotnet 的 PATH（官方脚本会把 PATH 写进 .bashrc）
touch /root/.bashrc 2>/dev/null && . /root/.bashrc 2>/dev/null || true
[ -f "$HOME/.bashrc" ] && . "$HOME/.bashrc" 2>/dev/null || true

# 用仓库根标记文件向上查找仓库根目录：stable(DefaultTasks/) 与 dev(DefaultTasks/dev/)
# 深度不同，但都能靠 Ray.BiliBiliTool.sln 这个根文件定位，从而共用同一份 base。
find_repo_root() {
    local dir="$1"
    while [ "$dir" != "/" ] && [ -n "$dir" ]; do
        if [ -f "$dir/Ray.BiliBiliTool.sln" ]; then
            echo "$dir"
            return 0
        fi
        dir="$(dirname "$dir")"
    done
    return 1
}

BILI_SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" >/dev/null 2>&1 && pwd)"
bilitool_repo_dir="$(find_repo_root "$BILI_SCRIPT_DIR" || true)"
# 兜底：找不到标记文件时按目录层级回退（DefaultTasks/ 上跳两级）
if [ -z "$bilitool_repo_dir" ]; then
    bilitool_repo_dir="$(cd "$BILI_SCRIPT_DIR/../.." >/dev/null 2>&1 && pwd)"
fi
say "bilitool仓库目录: $bilitool_repo_dir"

current_linux_os="debian"  # 或 alpine
current_os="linux"         # 或 linux-musl
machine_architecture="x64" # 或 arm、arm64

bilitool_installed_version=0

# 所有二进制相关操作都在仓库根的 bin 目录下执行
cd "$bilitool_repo_dir"
mkdir -p bin && cd "$bilitool_repo_dir/bin"

machine_has() {
    eval $invocation
    command -v "$1" >/dev/null 2>&1
    return $?
}

# 输出：arm、arm64、x64
get_machine_architecture() {
    eval $invocation
    if command -v uname >/dev/null; then
        CPUName=$(uname -m)
        case $CPUName in
        armv*l)
            echo "arm"
            return 0
            ;;
        aarch64 | arm64)
            echo "arm64"
            return 0
            ;;
        esac
    fi
    echo "x64"
    return 0
}

get_linux_platform_name() {
    eval $invocation
    if [ -e /etc/os-release ]; then
        . /etc/os-release
        echo "$ID${VERSION_ID:+.${VERSION_ID}}"
        return 0
    fi
    echo "Linux specific platform name and version could not be detected"
    return 1
}

is_musl_based_distro() {
    eval $invocation
    (ldd --version 2>&1 || true) | grep -q musl
}

# 输出：linux、linux-musl
get_current_os_name() {
    eval $invocation
    local uname=$(uname)
    if [ "$uname" = "Linux" ]; then
        if is_musl_based_distro; then
            echo "linux-musl"
            return 0
        else
            echo "linux"
            return 0
        fi
    fi
    say_err "OS name could not be detected: UName = $uname"
    return 1
}

check_os() {
    eval $invocation
    current_os="$(get_current_os_name)"
    say "当前系统：$current_os"

    machine_architecture="$(get_machine_architecture)"
    say "当前架构：$machine_architecture"

    if [ "$current_os" = "linux" ]; then
        current_linux_os="debian"
        if ! machine_has curl; then
            say "curl未安装，开始安装依赖..."
            apt-get update && apt-get install -y curl
        fi
    else
        current_linux_os="alpine"
        if ! machine_has curl; then
            say "curl未安装，开始安装依赖..."
            apk update && apk add -y curl
        fi
    fi

    say "当前选择的运行方式：$prefer_mode"
}

check_jq() {
    if [ "$current_linux_os" = "debian" ]; then
        machine_has jq || { say "安装jq..."; apt-get update && apt-get install -y jq; }
    else
        machine_has jq || { say "安装jq..."; apk update && apk add -y jq; }
    fi
}

check_unzip() {
    if [ "$current_linux_os" = "debian" ]; then
        machine_has unzip || { say "安装unzip..."; apt-get update && apt-get install -y unzip; }
    else
        machine_has unzip || { say "安装unzip..."; apk update && apk add -y unzip; }
    fi
}

check_dotnet() {
    eval $invocation
    dotnetVersion=$(dotnet --version)
    say "当前dotnet版本：$dotnetVersion"
    if [[ $(echo "$dotnetVersion" | grep -oE '^[0-9]+') -ge 8 ]]; then
        say "已安装，且版本满足"
        say "which dotnet: $(which dotnet)"
        return 0
    else
        say "未安装"
        return 1
    fi
}

check_bilitool() {
    eval $invocation
    TAG_FILE="./tag.txt"
    touch $TAG_FILE
    local STORED_TAG=$(cat $TAG_FILE 2>/dev/null)
    if [[ -z $STORED_TAG ]]; then
        say "tag.txt为空，未安装过"
        return 1
    fi
    say "tag.txt记录的版本：$STORED_TAG"
    if [ -f "./Ray.BiliBiliTool.Console" ]; then
        say "bilitool已安装"
        bilitool_installed_version=$STORED_TAG
        return 0
    else
        say "bilitool未安装"
        return 1
    fi
}

check_installed() {
    eval $invocation
    if [ "$prefer_mode" == "dotnet" ]; then
        check_dotnet
        return $?
    fi
    if [ "$prefer_mode" == "bilitool" ]; then
        check_bilitool
        return $?
    fi
    return 1
}

install_dotnet_by_script() {
    eval $invocation
    say "再尝试使用官方脚本安装"
    curl -sSL https://dot.net/v1/dotnet-install.sh | bash /dev/stdin --channel 8.0 --verbose

    say "添加到PATH"
    local exportFile="/root/.bashrc"
    touch $exportFile
    echo '' >>$exportFile
    echo 'export DOTNET_ROOT=$HOME/.dotnet' >>$exportFile
    echo 'export PATH=$PATH:$DOTNET_ROOT:$DOTNET_ROOT/tools' >>$exportFile
    . $exportFile
}

install_dotnet() {
    eval $invocation
    say "开始安装dotnet"
    say "当前系统：$current_linux_os"
    if [[ $current_linux_os == "debian" ]]; then
        say "使用apt安装"
        if ! (curl -s -m 5 www.google.com >/dev/null); then
            say "机器位于墙内，切换为国内镜像源"
            cp /etc/apt/sources.list /etc/apt/sources.list.bak 2>/dev/null || true
            sed -i 's/https:\/\/deb.debian.org/https:\/\/mirrors.ustc.edu.cn/g' /etc/apt/sources.list 2>/dev/null || true
            sed -i 's/http:\/\/deb.debian.org/https:\/\/mirrors.ustc.edu.cn/g' /etc/apt/sources.list 2>/dev/null || true
            apt-get update
        fi
        {
            . /etc/os-release
            curl -o packages-microsoft-prod.deb https://packages.microsoft.com/config/debian/$VERSION_ID/packages-microsoft-prod.deb
            dpkg -i packages-microsoft-prod.deb
            rm packages-microsoft-prod.deb
            apt-get update && apt-get install -y dotnet-sdk-8.0
        } || {
            install_dotnet_by_script
        }
    else
        say "使用apk安装"
        if ! (curl -s -m 5 www.google.com >/dev/null); then
            say "机器位于墙内，切换为国内镜像源"
            cp /etc/apk/repositories /etc/apk/repositories.bak 2>/dev/null || true
            sed -i 's/https:\/\/dl-cdn.alpinelinux.org/https:\/\/mirrors.ustc.edu.cn/g' /etc/apk/repositories 2>/dev/null || true
            sed -i 's/http:\/\/dl-cdn.alpinelinux.org/https:\/\/mirrors.ustc.edu.cn/g' /etc/apk/repositories 2>/dev/null || true
            apk update
        fi
        {
            apk add dotnet8-sdk
        } || {
            install_dotnet_by_script
        }
    fi
    dotnet --version && say "which dotnet: $(which dotnet)" && say "安装成功"
    return $?
}

get_download_url() {
    eval $invocation
    tag=$1
    url="${github_proxy}https://github.com/RayWangQvQ/BiliBiliToolPro/releases/download/$tag/bilibili-tool-pro-v$tag-$current_os-$machine_architecture.zip"
    say "下载地址：$url"
    echo $url
    return 0
}

install_bilitool() {
    eval $invocation
    say "开始安装bilitool"
    LATEST_RELEASE=$(curl -s https://api.github.com/repos/$bili_repo/releases/latest)
    check_jq
    LATEST_TAG=$(echo $LATEST_RELEASE | jq -r '.tag_name')
    say "最新版本：$LATEST_TAG"

    if [ "$LATEST_TAG" != "$bilitool_installed_version" ]; then
        ASSET_URL=$(get_download_url $LATEST_TAG)
        local zip_file_name="bilitool-$LATEST_TAG.zip"
        curl -L -o "$zip_file_name" $ASSET_URL
        check_unzip
        unzip -jo "$zip_file_name" -d ./ &&
            rm "$zip_file_name" &&
            rm -f appsettings.*
        echo $LATEST_TAG >./tag.txt
    else
        say "已经是最新版本，无需下载。"
    fi
}

install() {
    eval $invocation
    if check_installed; then
        say "环境正常，本次无需安装"
    else
        say "开始安装环境"
        if [ "$prefer_mode" == "dotnet" ]; then
            install_dotnet || {
                say_err "安装失败，请根据文档自行在面板容器中安装dotnet，或切换为 bilitool 模式"
                say_err "文档：https://github.com/RayWangQvQ/BiliBiliToolPro/blob/develop/daidai/README.md"
            }
        fi
        if [ "$prefer_mode" == "bilitool" ]; then
            install_bilitool || {
                say_err "安装失败，请检查日志并重试，或切换为 dotnet 模式"
                say_err "文档：https://github.com/RayWangQvQ/BiliBiliToolPro/blob/develop/daidai/README.md"
            }
        fi
    fi
}

# 运行 bilitool 任务
run_task() {
    eval $invocation
    local target_code=$1

    export Ray_PlatformType=DaiDai
    export Ray_RunTasks=$target_code

    if [ "$prefer_mode" == "dotnet" ]; then
        cd "$bilitool_repo_dir/src/Ray.BiliBiliTool.Console"
        # 临时 props 文件压制编译告警，保持日志整洁，运行后删除
        local props_file="$bilitool_repo_dir/Directory.Build.props"
        local props_created=false
        if [ ! -f "$props_file" ]; then
            printf '<Project>\n  <PropertyGroup>\n    <NoWarn>$(NoWarn);NETSDK1188;CS9057;CS8618;CS9042;CS8625;CS8603;CS8602;CS8601;CS8600;CS8604</NoWarn>\n  </PropertyGroup>\n</Project>' >"$props_file"
            props_created=true
        fi
        dotnet run -v m -- --ENVIRONMENT=Production
        [ "$props_created" = true ] && rm -f "$props_file"
    else
        cd "$bilitool_repo_dir/bin"
        chmod +x ./Ray.BiliBiliTool.Console && ./Ray.BiliBiliTool.Console --ENVIRONMENT=Production
    fi
}

check_os
install
