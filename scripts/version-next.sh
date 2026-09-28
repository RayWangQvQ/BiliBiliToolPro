#!/usr/bin/env bash
# 新发布策略（ADR-0002）的算号入口：版本号由 git tag 历史推导，
# 任何文件都不是权威源，本地构建恒为 0.0.0-dev，CI 一律用本脚本显式注入。
#
#   alpha           -> <上个稳定tag的下一patch>-alpha.N，N=自该 tag 以来的提交数
#   stable <level>  -> 上个稳定 tag 按 patch|minor|major bump
#   latest          -> 最新稳定 tag（无则输出空）
#
# 测试 seam：VERSION_REPO_DIR 可指向任意 git 仓库（fixture 测试用），
# 缺省为本脚本所在仓库。CI 需要 fetch-depth: 0，否则看不到 tag。
#
# 注意：alpha 序号 = 自上个稳定 tag 以来的提交数，依赖每次合并都真实触发。
# 若提交信息/PR 标题里出现 CI-skip 标记（[skip ci] 等字面量），GitHub 会整体
# 跳过那次 push 的全部 workflow —— alpha 构建静默缺失、序号断号，且不报任何错。
set -euo pipefail

repo_dir=${VERSION_REPO_DIR:-$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)}

die() {
    echo "$1" >&2
    exit 1
}

# 稳定 tag：纯数字三段式。v 前缀的历史异类和 -alpha.N 预发布都不算。
stable_tags() {
    git -C "$repo_dir" tag -l | grep -E '^[0-9]+\.[0-9]+\.[0-9]+$' | sort -V || true
}

latest_stable() {
    stable_tags | tail -1
}

# 无稳定 tag 的兜底（仓库首次发版场景）：版本线从 0.1.0 起步，
# 此时 stable 无论选什么级别都是 0.1.0，alpha 基底也是 0.1.0。
default_base="0.1.0"

bump() { # <版本> <patch|minor|major>
    awk -F. -v level="$2" '{
        if (level == "major")      printf "%d.0.0", $1 + 1
        else if (level == "minor") printf "%d.%d.0", $1, $2 + 1
        else                       printf "%d.%d.%d", $1, $2, $3 + 1
    }' <<< "$1"
}

alpha() {
    local latest base n
    latest=$(latest_stable)
    if [ -z "$latest" ]; then
        base=$default_base
        n=$(git -C "$repo_dir" rev-list --count HEAD)
    else
        base=$(bump "$latest" patch)
        n=$(git -C "$repo_dir" rev-list --count "$latest..HEAD")
    fi
    # squash 合并保证 N 等于合并的 PR 数；发稳定版后计数自然复位。
    [ "$n" -ge 1 ] || die "自 ${latest:-仓库起点} 以来没有新提交，没有可出的 alpha"
    echo "$base-alpha.$n"
}

stable() {
    local level=${1:-} latest
    case "$level" in
    patch | minor | major) ;;
    *) die "未知升级级别: '${level}'（期望 patch|minor|major）" ;;
    esac
    latest=$(latest_stable)
    if [ -z "$latest" ]; then
        echo "$default_base"
    else
        bump "$latest" "$level"
    fi
}

case "${1:-help}" in
alpha) alpha ;;
stable) stable "${2:-}" ;;
latest) latest_stable ;;
*)
    cat <<EOF
用法：$0 {alpha|stable <patch|minor|major>|latest}
  alpha    当前 HEAD 应产出的 alpha 版本（无新提交则报错）
  stable   按级别算下一个稳定版本
  latest   最新稳定 tag，无则输出空
环境变量 VERSION_REPO_DIR 可指定目标 git 仓库（测试用）。
EOF
    ;;
esac
