#!/usr/bin/env bash
# cron:0 0 1 1 *
# new Env("bili尝试修复异常")

bili_repo="raywangqvq_bilibilitoolpro"
bili_branch=""

set -e
QL_DIR=${QL_DIR:-"/ql"}
dir_shell="$QL_DIR/shell"
. "$dir_shell/share.sh" || exit 1
touch /root/.bashrc && . /root/.bashrc

dir_repo=${dir_repo:-"$QL_DIR/data/repo"}
if [ ! -d "$dir_repo" ] && [ -d "$QL_DIR/repo" ]; then
    dir_repo="$QL_DIR/repo"
fi

# Keep this resolver self-contained: Qinglong copies task scripts independently.
is_bili_repo() {
    local repo_root="$1"
    local console_dir="$repo_root/src/Ray.BiliBiliTool.Console"
    [[ -f "$console_dir/Ray.BiliBiliTool.Console.csproj" ]] || return 1
    [[ ! -L "$console_dir/Ray.BiliBiliTool.Console.csproj" ]] || return 1
    local resolved_console
    resolved_console="$(cd -P -- "$console_dir" && pwd)" || return 1
    [[ "$resolved_console" == "$console_dir" ]]
}

get_bili_repo_dir() {
    local script_path source_root
    script_path="$(readlink -f -- "${BASH_SOURCE[0]}")" || return 1
    source_root="$(cd -P -- "$(dirname -- "$script_path")/../../.." && pwd)" || return 1
    if [[ -z "${bili_branch:-}" ]] && is_bili_repo "$source_root"; then
        printf '%s\n' "$source_root"
        return 0
    fi

    local repo_parent candidate resolved selected=""
    if [[ ! -d "$dir_repo" ]]; then
        echo "未找到青龙仓库目录，请先完成拉库。" >&2
        return 1
    fi
    repo_parent="$(cd -P -- "$dir_repo" && pwd)" || return 1
    # Inspect immediate repositories only; never traverse links or nested backups.
    for candidate in "$repo_parent"/*; do
        [[ -d "$candidate" ]] || continue
        # Preserve the existing inline branch override for copied task scripts.
        if [[ -n "${bili_branch:-}" ]]; then
            local requested_name="${bili_repo//\//_}${bili_branch}"
            local candidate_name="${candidate##*/}"
            if [[ "${candidate_name,,}" != "${requested_name,,}" && "${candidate_name,,}" != "${requested_name,,}_main" ]]; then
                continue
            fi
        fi
        resolved="$(cd -P -- "$candidate" && pwd)" || continue
        [[ "$resolved" == "$repo_parent/"* ]] || continue
        is_bili_repo "$resolved" || continue
        if [[ -n "$selected" && "$selected" != "$resolved" ]]; then
            echo "找到多个 BiliTool 仓库，无法确认任务来源。请从目标仓库中的任务脚本运行。" >&2
            return 1
        fi
        selected="$resolved"
    done
    if [[ -z "$selected" ]]; then
        echo "未找到完整的 BiliTool 仓库，请重新拉库后再运行。" >&2
        return 1
    fi
    printf '%s\n' "$selected"
}

echo "青龙repo目录: $dir_repo"
if ! qinglong_bili_repo_dir="$(get_bili_repo_dir)"; then
    echo "仓库定位失败，未清理任何缓存。" >&2
    exit 1
fi
echo "bili仓库目录: $qinglong_bili_repo_dir"

echo -e "清理缓存...\n"
cd -- "$qinglong_bili_repo_dir" || exit 1
# find does not follow symlinks; prune prevents traversing nested cache trees.
find . -type d \( -name "bin" -o -name "obj" \) -prune -exec rm -rf -- {} +
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