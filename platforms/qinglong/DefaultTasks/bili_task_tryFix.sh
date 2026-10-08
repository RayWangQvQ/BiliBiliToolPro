#!/usr/bin/env bash
# cron:0 0 1 1 *
# new Env("bili尝试修复异常")

script_dir="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
. "$script_dir/bili_task_bilitool_lock.sh"

dir_shell=$QL_DIR/shell
. $dir_shell/share.sh
. /root/.bashrc

bili_repo=${BILI_REPO:-"raywangqvq/bilibilitoolpro"}
bili_branch=${BILI_BRANCH:-""}

echo "青龙repo目录: $dir_repo"
qinglong_bili_repo="$(echo "$bili_repo" | sed 's/\//_/g')${bili_branch}"
qinglong_bili_repo_dir="$(find "$dir_repo" -type d \( -iname "$qinglong_bili_repo" -o -iname "${qinglong_bili_repo}_main" \) | head -1)"
echo "bili仓库目录: $qinglong_bili_repo_dir"

if [ -z "$qinglong_bili_repo_dir" ]; then
    echo "未找到 bili 仓库目录"
    echo "查找目标：$qinglong_bili_repo"
    echo "请确认已在青龙中拉取仓库 ${bili_repo}${bili_branch}，或通过环境变量 BILI_REPO / BILI_BRANCH 覆盖默认值"
    exit 1
fi

lock_wait_seconds="${BILITOOL_LOCK_WAIT_SECONDS:-7200}"
lock_key="$(bilitool_lock_key "$qinglong_bili_repo_dir" "$bili_branch")"
lock_file="/tmp/bilitool-${lock_key}.lock"
if ! command -v flock >/dev/null 2>&1; then
    echo "缺少flock命令，请安装util-linux后重试"
    exit 1
fi
if ! acquire_bilitool_lock "$lock_file" "$lock_wait_seconds"; then
    echo "获取BiliBiliTool锁失败，已等待 ${lock_wait_seconds} 秒"
    exit 1
fi

echo -e "清理缓存...\n"
cd "$qinglong_bili_repo_dir"
find . -type d -name "bin" -exec rm -rf {} +
find . -type d -name "obj" -exec rm -rf {} +
echo -e "清理完成\n"

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

echo "检测dotnet..."
requiredDotnetMajor=10
dotnetMajor="$(get_dotnet_major_version dotnet || true)"
echo "当前dotnet主版本：${dotnetMajor:-未检测到可用SDK}"
if [[ "$dotnetMajor" =~ ^[0-9]+$ && "$dotnetMajor" -ge "$requiredDotnetMajor" ]]; then
    echo "已安装，且版本满足"
else
    echo "which dotnet: $(which dotnet)"
    echo "Path: $PATH"
    if ! bash "$qinglong_bili_repo_dir/platforms/qinglong/ray-dotnet-install.sh"; then
        echo "安装 .NET $requiredDotnetMajor SDK 失败"
        exit 1
    fi
    . /root/.bashrc
    dotnetMajor="$(get_dotnet_major_version dotnet || true)"
    if ! [[ "$dotnetMajor" =~ ^[0-9]+$ && "$dotnetMajor" -ge "$requiredDotnetMajor" ]]; then
        echo ".NET $requiredDotnetMajor SDK 安装后不可用"
        exit 1
    fi
    echo "当前dotnet主版本：$dotnetMajor"
fi
echo "检测dotnet结束"
