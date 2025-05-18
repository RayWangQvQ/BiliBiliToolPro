#!/usr/bin/env bash
# cron:0 0 1 1 *
# new Env("bili_base")

# Stop script on NZEC
set -e
# Stop script if unbound variable found (use ${var:-} if intentional)
set -u
# By default cmd1 | cmd2 returns exit code of cmd2 regardless of cmd1 success
# This is causing it to fail
set -o pipefail

verbose=false                          # 开启debug日志
bili_repo="raywangqvq/bilibilitoolpro" # 仓库地址
bili_branch=""                         # 分支名，空或_develop
prefer_mode=${BILI_MODE:-"dotnet"}     # dotnet或bilitool，需要通过环境变量配置
github_proxy=${BILI_GITHUB_PROXY:-""}  # 下载github release包时使用的代理，会拼在地址前面，需要通过环境变量配置
export DOTNET_SYSTEM_GLOBALIZATION_INVARIANT=1 # 解决抽风问题

# Use in the the functions: eval $invocation
invocation='say_verbose "Calling: ${yellow:-}${FUNCNAME[0]} ${green:-}$*${normal:-}"'

# standard output may be used as a return value in the functions
# we need a way to write text on the screen in the functions so that
# it won't interfere with the return value.
# Exposing stream 3 as a pipe to standard output of the script itself
exec 3>&1

# Setup some colors to use. These need to work in fairly limited shells, like the Ubuntu Docker container where there are only 8 colors.
# See if stdout is a terminal
if [ -t 1 ] && command -v tput >/dev/null; then
    # see if it supports colors
    ncolors=$(tput colors || echo 0)
    if [ -n "$ncolors" ] && [ $ncolors -ge 8 ]; then
        bold="$(tput bold || echo)"
        normal="$(tput sgr0 || echo)"
        black="$(tput setaf 0 || echo)"
        red="$(tput setaf 1 || echo)"
        green="$(tput setaf 2 || echo)"
        yellow="$(tput setaf 3 || echo)"
        blue="$(tput setaf 4 || echo)"
        magenta="$(tput setaf 5 || echo)"
        cyan="$(tput setaf 6 || echo)"
        white="$(tput setaf 7 || echo)"
    fi
fi

say_warning() {
    printf "%b\n" "${yellow:-}bilitool: Warning: $1${normal:-}" >&3
}

say_err() {
    printf "%b\n" "${red:-}bilitool: Error: $1${normal:-}" >&2
}

say() {
    # using stream 3 (defined in the beginning) to not interfere with stdout of functions
    # which may be used as return value
    printf "%b\n" "${cyan:-}bilitool:${normal:-} $1" >&3
}

say_verbose() {
    if [ "$verbose" = true ]; then
        say "$1"
    fi
}

QL_DIR=${QL_DIR:-"/ql"}
QL_BRANCH=${QL_BRANCH:-"develop"}
DefaultCronRule=${DefaultCronRule:-""}
CpuWarn=${CpuWarn:-""}
MemoryWarn=${MemoryWarn:-""}
DiskWarn=${DiskWarn:-""}

dir_repo=${dir_repo:-"$QL_DIR/data/repo"}
# 需要兼容老版本青龙，https://github.com/RayWangQvQ/BiliBiliToolPro/issues/728
if [ ! -d "$dir_repo" ] && [ -d "$QL_DIR/repo" ]; then
  dir_repo="$QL_DIR/repo"
fi
dir_shell=$QL_DIR/shell
touch $dir_shell/env.sh && . $dir_shell/env.sh
touch /root/.bashrc && . /root/.bashrc

# 目录
say "青龙repo目录: $dir_repo"
qinglong_bili_repo="$(echo "$bili_repo" | sed 's/\//_/g')${bili_branch}"
qinglong_bili_repo_dir="$(find $dir_repo -type d \( -iname $qinglong_bili_repo -o -iname ${qinglong_bili_repo}_main \) | head -1)"
say "bili仓库目录: $qinglong_bili_repo_dir"

current_linux_os="debian"  # 或alpine
current_os="linux"         # 或linux-musl
machine_architecture="x64" # 或arm、arm64

bilitool_installed_version=0

# 以下操作仅在bilitool仓库的根bin文件下执行
cd $qinglong_bili_repo_dir
mkdir -p bin && cd $qinglong_bili_repo_dir/bin

# 判断是否存在某指令
machine_has() {
    eval $invocation

    command -v "$1" >/dev/null 2>&1
    return $?
}

# 判断系统架构
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

    # Always default to 'x64'
    echo "x64"
    return 0
}

# 获取linux系统名称
# 输出：debian.10、debian.11、debian.12、ubuntu.20.04、ubuntu.22.04、alpine.3.4.3...
get_linux_platform_name() {
    eval $invocation

    if [ -e /etc/os-release ]; then
        . /etc/os-release
        echo "$ID${VERSION_ID:+.${VERSION_ID}}"
        return 0
    elif [ -e /etc/redhat-release ]; then
        local redhatRelease=$(</etc/redhat-release)
        if [[ $redhatRelease == "CentOS release 6."* || $redhatRelease == "Red Hat Enterprise Linux "*" release 6."* ]]; then
            echo "rhel.6"
            return 1
        fi
    fi

    echo "Linux specific platform name and version could not be detected: UName = $uname"
    return 1
}

# 判断是否为musl（一般指alpine）
is_musl_based_distro() {
    eval $invocation

    (ldd --version 2>&1 || true) | grep -q musl
}

# 获取当前系统名称
# 输出：linux、linux-musl、osx、freebsd
get_current_os_name() {
    eval $invocation

    local uname=$(uname)
    if [ "$uname" = "Darwin" ]; then
        say_warning "当前系统：osx"
        echo "osx"
        return 1
    elif [ "$uname" = "FreeBSD" ]; then
        say_warning "当前系统：freebsd"
        echo "freebsd"
        return 1
    elif [ "$uname" = "Linux" ]; then
        local linux_platform_name=""
        linux_platform_name="$(get_linux_platform_name)" || true
        say "当前系统发行版本：$linux_platform_name"

        if [ "$linux_platform_name" = "rhel.6" ]; then
            echo $linux_platform_name
            return 1
        elif is_musl_based_distro; then
            echo "linux-musl"
            return 0
        elif [ "$linux_platform_name" = "linux-musl" ]; then
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

# 检查操作系统
check_os() {
    eval $invocation

    current_os="$(get_current_os_name)"
    say "当前系统：$current_os"

    machine_architecture="$(get_machine_architecture)"
    say "当前架构：$machine_architecture"

    if [ "$current_os" = "linux" ]; then
        current_linux_os="debian" # 当前青龙只有debian和aplpine两种
        if ! machine_has curl; then
            say "curl未安装，开始安装依赖..."
            apt-get update
            apt-get install -y curl
        fi
    else
        current_linux_os="alpine"
        if ! machine_has curl; then
            say "curl未安装，开始安装依赖..."
            apk update
            apk add -y curl
        fi
    fi

    say "当前选择的运行方式：$prefer_mode"
}

# 检查安装jq
check_jq() {
    if [ "$current_linux_os" = "debian" ]; then
        if ! machine_has jq; then
            say "jq未安装，开始安装依赖..."
            apt-get update
            apt-get install -y jq
        fi
    else
        if ! machine_has jq; then
            say "jq未安装，开始安装依赖..."
            apk update
            apk add -y jq
        fi
    fi
}

# 检查安装unzip
check_unzip() {
    if [ "$current_linux_os" = "debian" ]; then
        if ! machine_has unzip; then
            say "unzip未安装，开始安装依赖..."
            apt-get update
            apt-get install -y unzip
        fi
    else
        if ! machine_has unzip; then
            say "jq未安装，开始安装依赖..."
            apk update
            apk add -y unzip
        fi
    fi
}

# 检查dotnet
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

# 检查bilitool
check_bilitool() {
    eval $invocation

    TAG_FILE="./tag.txt"
    touch $TAG_FILE
    local STORED_TAG=$(cat $TAG_FILE 2>/dev/null)

    #如果STORED_TAG为空，则返回1
    if [[ -z $STORED_TAG ]]; then
        say "tag.txt为空，未安装过"
        return 1
    fi

    say "tag.txt记录的版本：$STORED_TAG"

    # 查找当前目录下是否有叫Ray.BiliBiliTool.Console的文件
    if [ -f "./Ray.BiliBiliTool.Console" ]; then
        say "bilitool已安装"
        bilitool_installed_version=$STORED_TAG
        return 0
    else
        say "bilitool未安装"
        return 1
    fi
}

# 检查环境
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

# 使用官方脚本安装dotnet
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

# 安装dotnet环境
install_dotnet() {
    eval $invocation

    say "开始安装dotnet"
    say "当前系统：$current_linux_os"
    if [[ $current_linux_os == "debian" ]]; then
        say "使用apt安装"

        if ! (curl -s -m 5 www.google.com >/dev/nul); then
            say "机器位于墙内，切换为包源为国内镜像源"
            cp /etc/apt/sources.list /etc/apt/sources.list.bak
            sed -i 's/https:\/\/deb.debian.org/https:\/\/mirrors.ustc.edu.cn/g' /etc/apt/sources.list
            sed -i 's/http:\/\/deb.debian.org/https:\/\/mirrors.ustc.edu.cn/g' /etc/apt/sources.list
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
        if ! (curl -s -m 5 www.google.com >/dev/nul); then
            say "机器位于墙内，切换为包源为国内镜像源"
            cp /etc/apk/repositories /etc/apk/repositories.bak
            sed -i 's/https:\/\/dl-cdn.alpinelinux.org/https:\/\/mirrors.ustc.edu.cn/g' /etc/apk/repositories
            sed -i 's/http:\/\/dl-cdn.alpinelinux.org/https:\/\/mirrors.ustc.edu.cn/g' /etc/apk/repositories
            apk update
        fi
        {
            apk add dotnet8-sdk # https://pkgs.alpinelinux.org/packages
        } || {
            install_dotnet_by_script
        }
    fi
    dotnet --version && say "which dotnet: $(which dotnet)" && say "安装成功"
    return $?
}

# 从github获取bilitool下载地址
get_download_url() {
    eval $invocation

    tag=$1
    url="${github_proxy}https://github.com/RayWangQvQ/BiliBiliToolPro/releases/download/$tag/bilibili-tool-pro-v$tag-$current_os-$machine_architecture.zip"
    say "下载地址：$url"
    echo $url
    return 0
}

# 安装bilitool
install_bilitool() {
    eval $invocation

    say "开始安装bilitool"
    # 获取最新的release信息
    LATEST_RELEASE=$(curl -s https://api.github.com/repos/$bili_repo/releases/latest)

    # 解析最新的tag名称
    check_jq
    LATEST_TAG=$(echo $LATEST_RELEASE | jq -r '.tag_name')
    say "最新版本：$LATEST_TAG"

    # 读取之前存储的tag并比较
    if [ "$LATEST_TAG" != "$bilitool_installed_version" ]; then
        # 如果不一样，则需要更新安装
        ASSET_URL=$(get_download_url $LATEST_TAG)

        # 使用curl下载文件到当前目录下的test.zip文件
        local zip_file_name="bilitool-$LATEST_TAG.zip"
        curl -L -o "$zip_file_name" $ASSET_URL

        # 解压
        check_unzip
        unzip -jo "$zip_file_name" -d ./ &&
            rm "$zip_file_name" &&
            rm -f appsettings.*

        # 更新tag.txt文件
        echo $LATEST_TAG >./tag.txt
    else
        say "已经是最新版本，无需下载。"
    fi
}

## 安装dotnet（如果未安装过）
install() {
    eval $invocation

    if check_installed; then
        say "环境正常，本次无需安装"
    else
        say "开始安装环境"
        if [ "$prefer_mode" == "dotnet" ]; then
            install_dotnet || {
                say_err "安装失败"
                say_err "请根据文档自行在青龙容器中安装dotnet：https://learn.microsoft.com/zh-cn/dotnet/core/install/linux-$current_linux_os"
                say_err "或者尝试切换运行模式为bilitool，它不需要安装dotnet：https://github.com/RayWangQvQ/BiliBiliToolPro/blob/develop/qinglong/README.md"
            }
        fi

        if [ "$prefer_mode" == "bilitool" ]; then
            install_bilitool || {
                say_err "安装失败，请检查日志并重试"
                say_err "或者尝试切换运行模式为dotnet：https://github.com/RayWangQvQ/BiliBiliToolPro/blob/develop/qinglong/README.md"
            }
        fi
    fi
}

# 运行bilitool任务
run_task() {
    eval $invocation

    local target_code=$1

    export Ray_PlatformType=QingLong
    export Ray_RunTasks=$target_code

    cd $qinglong_bili_repo_dir/src/Ray.BiliBiliTool.Console

    if [ "$prefer_mode" == "dotnet" ]; then
        dotnet run --ENVIRONMENT=Production
    else
        cp -f $qinglong_bili_repo_dir/bin/Ray.BiliBiliTool.Console .
        chmod +x ./Ray.BiliBiliTool.Console && ./Ray.BiliBiliTool.Console --ENVIRONMENT=Production
    fi
}

check_os
install
