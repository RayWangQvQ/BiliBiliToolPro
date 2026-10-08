#!/usr/bin/env bash
# new Env("bili_base")

verbose=false                                 # 开启debug日志
bili_repo=${BILI_REPO:-"raywangqvq/bilibilitoolpro"} # 仓库地址
bili_branch=${BILI_BRANCH:-""}                # 分支名，空或_develop
prefer_mode=${BILI_MODE:-"dotnet"}            # dotnet或bilitool，需要通过环境变量配置
github_proxy=${BILI_GITHUB_PROXY:-""}         # 下载github release包时使用的代理，会拼在地址前面，需要通过环境变量配置
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

script_dir="$(cd "$(dirname "$BASH_SOURCE")" && pwd)"
. "$script_dir/bili_task_bilitool_lock.sh"

current_linux_os="debian"  # 或alpine
current_os="linux"         # 或linux-musl
machine_architecture="x64" # 或arm、arm64
bilitool_installed_version=0
library_only="${BILITOOL_BASE_LIBRARY_ONLY-false}"
bilitool_runtime_pid=""
bilitool_lock_waiter_pid=""
bilitool_stop_requested=false
bilitool_job_pid=""
bilitool_job_signal_status=0

wait_for_managed_process_exit() {
    local pid="$1"
    local grace_seconds="${BILITOOL_STOP_GRACE_SECONDS:-10}"
    local elapsed=0

    case "$grace_seconds" in
        ''|*[!0-9]*) grace_seconds=10 ;;
    esac

    while kill -0 "$pid" 2>/dev/null && [ "$elapsed" -lt "$grace_seconds" ]; do
        sleep 1
        elapsed=$((elapsed + 1))
    done
    if kill -0 "$pid" 2>/dev/null; then
        say_warning "任务在 ${grace_seconds} 秒内未退出，发送 KILL"
        kill -KILL "$pid" 2>/dev/null || true
    fi
}

stop_bilitool_children() {
    local signal_name="${1:-TERM}"
    bilitool_stop_requested=true

    if [ -n "${bilitool_lock_waiter_pid:-}" ] && kill -0 "$bilitool_lock_waiter_pid" 2>/dev/null; then
        kill -"$signal_name" "$bilitool_lock_waiter_pid" 2>/dev/null || true
        wait "$bilitool_lock_waiter_pid" 2>/dev/null || true
        bilitool_lock_waiter_pid=""
    fi
    if [ -n "${bilitool_runtime_pid:-}" ] && kill -0 "$bilitool_runtime_pid" 2>/dev/null; then
        kill -"$signal_name" "$bilitool_runtime_pid" 2>/dev/null || true
        wait_for_managed_process_exit "$bilitool_runtime_pid"
        wait "$bilitool_runtime_pid" 2>/dev/null || true
        bilitool_runtime_pid=""
    fi
}

run_managed_process() {
    bilitool_stop_requested=false
    "$@" 9>&- &
    bilitool_runtime_pid=$!
    local status=0
    if wait "$bilitool_runtime_pid"; then
        status=0
    else
        status=$?
    fi
    bilitool_runtime_pid=""
    return "$status"
}

should_check_bilitool_update() {
    local now_epoch="$1"
    local checked_epoch="$2"
    local interval_seconds="$3"

    if [[ ! "$now_epoch" =~ ^[0-9]+$ ]] || [[ ! "$interval_seconds" =~ ^[1-9][0-9]*$ ]]; then
        return 0
    fi
    if [ -z "$checked_epoch" ] || [[ ! "$checked_epoch" =~ ^[0-9]+$ ]]; then
        return 0
    fi

    local now_value=$((10#$now_epoch))
    local checked_value=$((10#$checked_epoch))
    local interval_value=$((10#$interval_seconds))
    [ "$now_value" -lt "$checked_value" ] || [ $((now_value - checked_value)) -ge "$interval_value" ]
}

write_atomic_file() {
    local target="$1"
    local value="$2"
    local temp_file

    temp_file="$(mktemp "${target}.tmp.XXXXXX")" || return 1
    if ! printf '%s\n' "$value" >"$temp_file"; then
        rm -f "$temp_file"
        return 1
    fi
    if ! mv -f "$temp_file" "$target"; then
        rm -f "$temp_file"
        return 1
    fi
}

initialize_bilitool_context() {
    QL_DIR="${QL_DIR:-/ql}"
    QL_BRANCH="${QL_BRANCH:-main}"
    DefaultCronRule="${DefaultCronRule:-}"
    CpuWarn="${CpuWarn:-}"
    MemoryWarn="${MemoryWarn:-}"
    DiskWarn="${DiskWarn:-}"

    dir_repo="${dir_repo:-$QL_DIR/data/repo}"
    # 需要兼容老版本青龙，https://github.com/RayWangQvQ/BiliBiliToolPro/issues/728
    if [ ! -d "$dir_repo" ] && [ -d "$QL_DIR/repo" ]; then
        dir_repo="$QL_DIR/repo"
    fi
    dir_shell="$QL_DIR/shell"
    touch "$dir_shell/env.sh" && . "$dir_shell/env.sh"
    touch /root/.bashrc && . /root/.bashrc

    say "青龙repo目录: $dir_repo"
    qinglong_bili_repo="$(echo "$bili_repo" | sed 's/\//_/g')${bili_branch}"
    qinglong_bili_repo_dir="$(find "$dir_repo" -type d \( -iname "$qinglong_bili_repo" -o -iname "${qinglong_bili_repo}_main" \) | head -1)"
    say "bili仓库目录: $qinglong_bili_repo_dir"

    if [ -z "$qinglong_bili_repo_dir" ]; then
        say_err "未找到 bili 仓库目录"
        say_err "查找目标：$qinglong_bili_repo"
        say_err "请确认已在青龙中拉取仓库 ${bili_repo}${bili_branch}，或通过环境变量 BILI_REPO / BILI_BRANCH 覆盖默认值"
        exit 1
    fi

    lock_wait_seconds="${BILITOOL_LOCK_WAIT_SECONDS:-7200}"
    lock_key="$(bilitool_lock_key "$qinglong_bili_repo_dir" "$bili_branch")"
    lock_file="/tmp/bilitool-${lock_key}.lock"
    if ! command -v flock >/dev/null 2>&1; then
        say_err "缺少flock命令，请安装util-linux后重试"
        exit 1
    fi
    if ! acquire_bilitool_lock "$lock_file" "$lock_wait_seconds"; then
        say_err "获取BiliBiliTool锁失败，已等待 ${lock_wait_seconds} 秒"
        exit 1
    fi

    cd "$qinglong_bili_repo_dir"
    mkdir -p bin
    cd "$qinglong_bili_repo_dir/bin"

    bilitool_runtime_link="$qinglong_bili_repo_dir/bin/.bilitool-current"
    bilitool_installed_dir="$qinglong_bili_repo_dir/bin"

    update_check_interval_seconds="${BILITOOL_UPDATE_CHECK_INTERVAL_SECONDS:-86400}"
    if [[ ! "$update_check_interval_seconds" =~ ^[1-9][0-9]*$ ]]; then
        say_warning "BILITOOL_UPDATE_CHECK_INTERVAL_SECONDS 无效，使用默认值 86400"
        update_check_interval_seconds=86400
    fi
    update_checked_file="$qinglong_bili_repo_dir/bin/.bilitool-update-checked-at"
}

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

get_dotnet_major_version() {
    local dotnet_command="${1:-dotnet}"
    local dotnet_version
    # Respect SDK selection (including global.json) and reject failed commands.
    dotnet_version="$("$dotnet_command" --version 2>/dev/null)" || return 1
    # Some wrappers return SDK-list rows instead of one selected version.
    printf '%s\n' "$dotnet_version" | awk '
        /^[[:space:]]*[0-9]+\.[0-9]+\.[0-9]+(-[[:alnum:].-]+)?(\+[[:alnum:].-]+)?([[:space:]]+\[[^]]+\])?[[:space:]]*$/ {
            split($1, version, ".")
            if (version[1] + 0 > highest) highest = version[1] + 0
        }
        END {
            if (highest > 0) print highest
            else exit 1
        }
    '
}

# 检查dotnet
check_dotnet() {
    eval $invocation

    local dotnetMajor
    dotnetMajor="$(get_dotnet_major_version dotnet || true)"
    say "当前dotnet主版本：${dotnetMajor:-未检测到可用SDK}"
    if [[ "$dotnetMajor" =~ ^[0-9]+$ && "$dotnetMajor" -ge 10 ]]; then
        say "已安装，且版本满足"
        say "which dotnet: $(which dotnet)"
        return 0
    else
        say "未安装"
        return 1
    fi
}

remove_legacy_dotnet_entry() {
    local legacyDotnet="/usr/local/bin/dotnet"
    if [[ -e "$legacyDotnet" || -L "$legacyDotnet" ]]; then
        local legacyDotnetMajor
        legacyDotnetMajor="$(get_dotnet_major_version "$legacyDotnet" || true)"
        if ! [[ "$legacyDotnetMajor" =~ ^[0-9]+$ && "$legacyDotnetMajor" -ge 10 ]]; then
            rm -f "$legacyDotnet"
            hash -r
        fi
    fi
}

# 检查bilitool
check_bilitool() {
    eval $invocation

    local runtime_dir="$qinglong_bili_repo_dir/bin"
    local runtime_link="${bilitool_runtime_link:-$qinglong_bili_repo_dir/bin/.bilitool-current}"
    if [ -e "$runtime_link" ]; then
        runtime_dir="$runtime_link"
    fi

    TAG_FILE="$runtime_dir/tag.txt"
    local STORED_TAG=""
    if [ -f "$TAG_FILE" ]; then
        STORED_TAG="$(cat "$TAG_FILE")"
    fi

    #如果STORED_TAG为空，则返回1
    if [[ -z $STORED_TAG ]]; then
        say "tag.txt为空，未安装过"
        return 1
    fi

    say "tag.txt记录的版本：$STORED_TAG"

    # 查找当前目录下是否有叫Ray.BiliBiliTool.Console的文件
    if [ -f "$runtime_dir/Ray.BiliBiliTool.Console" ]; then
        say "bilitool已安装"
        bilitool_installed_version=$STORED_TAG
        bilitool_installed_dir="$runtime_dir"
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
    curl --fail --show-error --location --retry 3 https://dot.net/v1/dotnet-install.sh | bash /dev/stdin --channel 10.0 --verbose

    say "添加到PATH"
    local exportFile="/root/.bashrc"
    touch $exportFile
    echo '' >>$exportFile
    echo 'export DOTNET_ROOT=$HOME/.dotnet' >>$exportFile
    echo 'export PATH=$DOTNET_ROOT:$DOTNET_ROOT/tools:$PATH' >>$exportFile
    . $exportFile
}

# 安装dotnet环境
install_dotnet() {
    eval $invocation

    say "开始安装dotnet"
    say "当前系统：$current_linux_os"
    if [[ $current_linux_os == "debian" ]]; then
        say "使用apt安装"

        {
            . /etc/os-release
            curl --fail --show-error --location --retry 3 -o packages-microsoft-prod.deb https://packages.microsoft.com/config/debian/$VERSION_ID/packages-microsoft-prod.deb
            dpkg -i packages-microsoft-prod.deb
            rm packages-microsoft-prod.deb
            apt-get update && apt-get install -y dotnet-sdk-10.0
            unset DOTNET_ROOT
            export PATH="/usr/bin:$PATH"
            hash -r
        } || {
            install_dotnet_by_script
        }
    else
        say "使用apk安装"
        . /etc/os-release
        if [[ "${VERSION_ID:-}" != 3.23* ]]; then
            say_err "Alpine ${VERSION_ID:-unknown} 不支持通过包管理器安装 .NET 10"
            say_err "请升级到 Alpine 3.23 或设置 BILI_MODE=bilitool"
            return 1
        fi
        if ! apk add --no-cache dotnet10-sdk; then
            say_err "无法从 Alpine 包源安装 .NET 10 SDK，请确认 community 仓库已启用"
            return 1
        fi
        unset DOTNET_ROOT
        export PATH="/usr/bin:$PATH"
        hash -r
    fi

    if check_dotnet; then
        remove_legacy_dotnet_entry
        say "安装成功"
        return 0
    fi

    return 1
}

# 从github获取bilitool下载地址
get_download_url() {
    eval $invocation

    tag=$1
    url="${github_proxy}https://github.com/${bili_repo}/releases/download/$tag/bilibili-tool-pro-v$tag-$current_os-$machine_architecture.zip"
    say "下载地址：$url"
    echo $url
    return 0
}

replace_bilitool_files_atomically() {
    local extracted_dir="$1"
    local staged_tag="$2"
    local _backup_dir="$3"
    local source
    local file_name
    local release_dir
    local runtime_link="${bilitool_runtime_link:-$qinglong_bili_repo_dir/bin/.bilitool-current}"
    local staged_link="$qinglong_bili_repo_dir/bin/.bilitool-current.tmp.$$"

    if ! release_dir="$(mktemp -d "$qinglong_bili_repo_dir/bin/.bilitool-release.XXXXXX")"; then
        return 1
    fi
    for source in "$extracted_dir"/* "$staged_tag"; do
        [ -f "$source" ] || continue
        file_name="$(basename "$source")"
        case "$file_name" in
            appsettings.*)
                continue
                ;;
        esac
        if ! cp -f "$source" "$release_dir/$file_name"; then
            rm -rf "$release_dir"
            return 1
        fi
    done

    if [ -e "$runtime_link" ] && [ ! -L "$runtime_link" ]; then
        rm -rf "$release_dir"
        return 1
    fi
    if ! ln -s "$release_dir" "$staged_link"; then
        rm -rf "$release_dir"
        return 1
    fi
    if ! mv -Tf "$staged_link" "$runtime_link"; then
        rm -rf "$staged_link"
        rm -rf "$release_dir"
        return 1
    fi
    bilitool_installed_dir="$runtime_link"
}

# 安装或更新bilitool
install_bilitool() {
    eval $invocation

    say "开始安装bilitool"
    local latest_release
    local latest_tag
    local asset_url
    local temp_dir
    local zip_file_name
    local staged_tag

    if ! latest_release="$(curl --fail --show-error --location --retry 3 "https://api.github.com/repos/$bili_repo/releases/latest")"; then
        say_err "无法获取GitHub最新版本信息"
        return 1
    fi

    if ! check_jq; then
        return 1
    fi
    if ! latest_tag="$(printf '%s' "$latest_release" | jq -r '.tag_name')"; then
        say_err "无法解析GitHub最新版本信息"
        return 1
    fi
    if [ -z "$latest_tag" ] || [ "$latest_tag" = "null" ]; then
        say_err "无法从GitHub获取有效的最新版本号"
        return 1
    fi
    say "最新版本：$latest_tag"

    if [ "$latest_tag" = "$bilitool_installed_version" ]; then
        say "已经是最新版本，无需下载。"
        return 0
    fi

    asset_url="$(get_download_url "$latest_tag")"
    if ! temp_dir="$(mktemp -d "$qinglong_bili_repo_dir/bin/.bilitool-update.XXXXXX")"; then
        say_err "无法创建bilitool更新临时目录"
        return 1
    fi
    zip_file_name="$temp_dir/bilitool-$latest_tag.zip"
    staged_tag="$temp_dir/tag.txt"

    if ! curl --fail --show-error --location --retry 3 -o "$zip_file_name" "$asset_url"; then
        rm -rf "$temp_dir"
        say_err "bilitool下载失败，保留当前版本"
        return 1
    fi
    if ! check_unzip; then
        rm -rf "$temp_dir"
        return 1
    fi
    mkdir -p "$temp_dir/extracted"
    if ! unzip -jo "$zip_file_name" -d "$temp_dir/extracted"; then
        rm -rf "$temp_dir"
        say_err "bilitool解压失败，保留当前版本"
        return 1
    fi
    if [ ! -f "$temp_dir/extracted/Ray.BiliBiliTool.Console" ]; then
        rm -rf "$temp_dir"
        say_err "下载包中未找到Ray.BiliBiliTool.Console"
        return 1
    fi
    printf '%s\n' "$latest_tag" >"$staged_tag"
    if ! replace_bilitool_files_atomically "$temp_dir/extracted" "$staged_tag" "$temp_dir/backup"; then
        rm -rf "$temp_dir"
        say_err "bilitool替换失败，保留当前版本"
        return 1
    fi
    rm -rf "$temp_dir"
}

update_bilitool_if_due() {
    local installed=false
    local checked_epoch=""
    local now_epoch

    if check_bilitool; then
        installed=true
    fi
    if [ -f "$update_checked_file" ]; then
        checked_epoch="$(cat "$update_checked_file")"
    fi
    if ! now_epoch="$(date +%s)"; then
        say_err "无法获取bilitool更新时间"
        return 1
    fi
    if [ "$installed" = true ] && ! should_check_bilitool_update "$now_epoch" "$checked_epoch" "$update_check_interval_seconds"; then
        say "bilitool更新检查仍在间隔内，跳过远端检查"
        return 0
    fi
    install_bilitool || return 1
    write_atomic_file "$update_checked_file" "$now_epoch"
}

## 安装dotnet（如果未安装过）
install() {
    eval $invocation

    if [ "$prefer_mode" == "bilitool" ]; then
        update_bilitool_if_due || {
            say_err "更新bilitool失败，请检查日志并重试"
            say_err "或者切换运行模式为dotnet：https://github.com/${bili_repo}/blob/main/platforms/qinglong/README.md"
            return 1
        }
    elif check_installed; then
        if [ "$prefer_mode" == "dotnet" ]; then
            remove_legacy_dotnet_entry
        fi
        say "环境正常，本次无需安装"
    else
        say "开始安装环境"
        if [ "$prefer_mode" == "dotnet" ]; then
            install_dotnet || {
                say_err "安装失败"
                say_err "请根据文档自行在青龙容器中安装dotnet：https://learn.microsoft.com/zh-cn/dotnet/core/install/linux-$current_linux_os"
                say_err "或者尝试切换运行模式为bilitool，它不需要安装dotnet：https://github.com/${bili_repo}/blob/main/platforms/qinglong/README.md"
                return 1
            }
        fi

    fi

    check_installed || {
        say_err "安装后验证失败"
        return 1
    }
}

publish_console() {
    local publish_dir="$qinglong_bili_repo_dir/bin/publish"
    local revision_file="$publish_dir/revision.txt"
    local current_revision
    current_revision=$(git -C "$qinglong_bili_repo_dir" rev-parse HEAD 2>/dev/null || echo unknown)
    local published_revision=""
    if [ -f "$revision_file" ]; then
        published_revision=$(cat "$revision_file")
    fi

    if [ ! -f "$publish_dir/Ray.BiliBiliTool.Console.dll" ] || [ "$published_revision" != "$current_revision" ]; then
        say "源码已更新，开始发布Console产物"
        dotnet publish "$qinglong_bili_repo_dir/src/Ray.BiliBiliTool.Console/Ray.BiliBiliTool.Console.csproj" -c Release -o "$publish_dir"
        echo "$current_revision" >"$revision_file"
    fi
}

# 运行bilitool任务
run_task() {
    eval $invocation

    # 兼容直接调用 run_task 的自定义脚本：不在 worker 内时改走完整作业流程
    if [ "${BILITOOL_TASK_IN_WORKER:-false}" != true ]; then
        run_bilitool_job "$@"
        return $?
    fi

    local target_code=$1

    export Ray_PlatformType=QingLong
    export Ray_RunTasks=$target_code

    cd "$qinglong_bili_repo_dir/src/Ray.BiliBiliTool.Console"

    if [ "$prefer_mode" == "dotnet" ]; then
        publish_console
        run_managed_process dotnet "$qinglong_bili_repo_dir/bin/publish/Ray.BiliBiliTool.Console.dll" --ENVIRONMENT=Production
    else
        cp -f "$bilitool_installed_dir/Ray.BiliBiliTool.Console" .
        chmod +x ./Ray.BiliBiliTool.Console
        run_managed_process ./Ray.BiliBiliTool.Console --ENVIRONMENT=Production
    fi
}

run_bilitool_worker() (
    set -euo pipefail
    BILITOOL_TASK_IN_WORKER=true
    trap 'stop_bilitool_children TERM; exit 143' TERM
    trap 'stop_bilitool_children INT; exit 130' INT
    trap 'stop_bilitool_children HUP; exit 129' HUP
    trap 'stop_bilitool_children TERM; release_bilitool_lock' EXIT

    initialize_bilitool_context
    check_os
    install
    run_task "$1"
)

restore_bilitool_trap() {
    local saved_trap="$1"
    local signal_name="$2"
    if [ -n "$saved_trap" ]; then
        eval "$saved_trap"
    else
        trap - "$signal_name"
    fi
}

forward_bilitool_job_signal() {
    local signal_name="$1"
    local status="$2"
    bilitool_job_signal_status="$status"
    if [ -n "${bilitool_job_pid:-}" ]; then
        stop_bilitool_job_group "$signal_name"
    fi
}

stop_bilitool_job_group() {
    local signal_name="${1:-TERM}"
    local grace_seconds="${BILITOOL_STOP_GRACE_SECONDS:-10}"
    local elapsed=0

    [ -n "${bilitool_job_pid:-}" ] || return 0
    case "$grace_seconds" in
        ''|*[!0-9]*) grace_seconds=10 ;;
    esac

    kill -"$signal_name" -- "-$bilitool_job_pid" 2>/dev/null || true
    while kill -0 -- "-$bilitool_job_pid" 2>/dev/null && [ "$elapsed" -lt "$grace_seconds" ]; do
        sleep 1
        elapsed=$((elapsed + 1))
    done
    if kill -0 -- "-$bilitool_job_pid" 2>/dev/null; then
        say_warning "任务进程组在 ${grace_seconds} 秒内未退出，发送 KILL"
        kill -KILL -- "-$bilitool_job_pid" 2>/dev/null || true
    fi
}

run_bilitool_job() {
    local saved_term_trap saved_int_trap saved_hup_trap status
    saved_term_trap="$(trap -p TERM)"
    saved_int_trap="$(trap -p INT)"
    saved_hup_trap="$(trap -p HUP)"
    bilitool_job_signal_status=0

    trap 'forward_bilitool_job_signal TERM 143' TERM
    trap 'forward_bilitool_job_signal INT 130' INT
    trap 'forward_bilitool_job_signal HUP 129' HUP

    if [ -n "${BILITOOL_TEST_BEFORE_WORKER_SPAWN_HOOK:-}" ]; then
        eval "$BILITOOL_TEST_BEFORE_WORKER_SPAWN_HOOK"
    fi
    if [ "$bilitool_job_signal_status" -eq 0 ]; then
        if ! command -v setsid >/dev/null 2>&1; then
            say_err "缺少setsid命令，请安装util-linux后重试"
            status=1
        else
            BILITOOL_BASE_SCRIPT="$script_dir/bili_task_base.sh" \
                BILITOOL_TARGET_CODE="$1" \
                setsid bash -c '
                    BILITOOL_BASE_LIBRARY_ONLY=true
                    . "$BILITOOL_BASE_SCRIPT"
                    if [ -n "${BILITOOL_TEST_WORKER_SETUP:-}" ]; then
                        . "$BILITOOL_TEST_WORKER_SETUP"
                    fi
                    run_bilitool_worker "$BILITOOL_TARGET_CODE"
                ' &
            bilitool_job_pid=$!
            if [ "$bilitool_job_signal_status" -ne 0 ]; then
                stop_bilitool_job_group TERM
            fi

            if wait "$bilitool_job_pid"; then
                status=0
            else
                status=$?
            fi
            if [ "$bilitool_job_signal_status" -ne 0 ]; then
                stop_bilitool_job_group TERM
                wait "$bilitool_job_pid" 2>/dev/null || true
            fi
        fi
    else
        status="$bilitool_job_signal_status"
    fi
    bilitool_job_pid=""

    restore_bilitool_trap "$saved_term_trap" TERM
    restore_bilitool_trap "$saved_int_trap" INT
    restore_bilitool_trap "$saved_hup_trap" HUP

    if [ "$bilitool_job_signal_status" -ne 0 ]; then
        return "$bilitool_job_signal_status"
    fi
    return "$status"
}
