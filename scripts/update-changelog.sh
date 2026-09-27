#!/usr/bin/env bash
# 发版时把新版本的 CHANGELOG 段落写到仓库 CHANGELOG.md 顶部（ADR-0002）。
# 重跑安全：若顶部已是同版本段落（上次发版在本步之前失败过），替换而不是重复插入。
#
#   update-changelog.sh <版本> [--since <tag>] [--from-file <文件>]
#
# --from-file 是测试 seam（透传给 release-notes.sh）。
set -euo pipefail

# 脚本自身位置（找兄弟脚本用）与目标仓库（VERSION_REPO_DIR 可覆盖，测试 seam）分开：
# fixture 测试里目标仓库是临时目录，但 release-notes.sh 永远在真实 scripts/ 下。
script_dir=$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)
repo_dir=${VERSION_REPO_DIR:-$(cd "$script_dir/.." && pwd)}
changelog="$repo_dir/CHANGELOG.md"

die() {
    echo "$1" >&2
    exit 1
}

version=${1:-}
[ -n "$version" ] || die "用法：$0 <版本> [--since <tag>] [--from-file <文件>]"
shift

extra_args=()
while [ $# -gt 0 ]; do
    case "$1" in
    --since | --from-file)
        extra_args+=("$1" "$2")
        shift 2
        ;;
    *) die "未知参数: $1" ;;
    esac
done

[ -f "$changelog" ] || die "$changelog 不存在"

section=$(VERSION_REPO_DIR="$repo_dir" bash "$script_dir/release-notes.sh" section "$version" "${extra_args[@]}")

first=$(grep -n '^## ' "$changelog" | head -1 | cut -d: -f1 || true)
second=$(grep -n '^## ' "$changelog" | sed -n 2p | cut -d: -f1 || true)

tmp=$(mktemp)
if [ -n "$first" ] && [ "$(sed -n "${first}p" "$changelog")" = "## $version" ]; then
    # 顶部已是本版本：替换该段落（保留其后所有历史段落）
    {
        echo "$section"
        [ -n "$second" ] && tail -n "+$second" "$changelog"
    } > "$tmp"
    echo "CHANGELOG 顶部已是 $version，已替换该段落"
else
    {
        echo "$section"
        cat "$changelog"
    } > "$tmp"
    echo "已在 CHANGELOG 顶部插入 $version 段落"
fi
mv "$tmp" "$changelog"
