#!/usr/bin/env bash
# 版本号与发布守卫的唯一实现，供 CI 和本地脚本共用。
set -euo pipefail
set -o pipefail

repo_dir=$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)

die() {
    echo "$1" >&2
    exit 1
}

prefix() {
    local p
    p=$(grep -oP '(?<=<VersionPrefix>).*?(?=</VersionPrefix>)' "$repo_dir/common.props" || true)
    [ -n "$p" ] || die "common.props 里找不到 <VersionPrefix>"
    echo "$p"
}

# 语义化版本比较：$1 > $2 时为 0
version_gt() {
    [ "$(printf '%s\n%s\n' "$1" "$2" | sort -V | tail -1)" = "$1" ] && [ "$1" != "$2" ]
}

# 已发布的正式版 tag（排除 -alpha 预发布和 v 前缀的历史 tag）
release_tags() {
    git -C "$repo_dir" tag -l | grep -E '^[0-9]+\.[0-9]+\.[0-9]+$' | sort -V || true
}

latest_release() {
    release_tags | tail -1
}

# CHANGELOG 顶部段落声明的版本号
changelog_top() {
    grep -oP '^## \K[0-9][^ ]*' "$repo_dir/CHANGELOG.md" | head -1
}

# CHANGELOG 顶部段落的正文，作为 GitHub Release 的说明
release_notes() {
    awk '/^## /{if (seen) exit; seen = 1; next} seen{print}' "$repo_dir/CHANGELOG.md"
}

# develop 上当前应使用的预览版本；无需构建时输出 SKIP
preview_tag() {
    local p latest last n
    p=$(prefix)
    latest=$(latest_release)
    if [ -n "$latest" ] && ! version_gt "$p" "$latest"; then
        echo "SKIP"
        echo "develop 的 VersionPrefix($p) 尚未推进过最新正式版($latest)，跳过预览镜像构建。请在 develop 上先 bump 版本号并开 CHANGELOG 新段。" >&2
        return 0
    fi
    last=$(git -C "$repo_dir" tag -l "$p-alpha.*" | sort -V | tail -1)
    if [ -z "$last" ]; then
        n=1
    else
        n=$(( ${last##*.} + 1 ))
    fi
    echo "$p-alpha.$n"
}

# main 上应发布的正式版本；已发布时输出 SKIP
release_tag() {
    local p
    p=$(prefix)
    if git -C "$repo_dir" rev-parse -q --verify "refs/tags/$p" >/dev/null; then
        echo "SKIP"
        echo "tag $p 已存在，无需重复发布。" >&2
        return 0
    fi
    echo "$p"
}

# 版本一致性：common.props 的前缀必须等于 CHANGELOG 顶部段落
check_consistency() {
    local p top
    p=$(prefix)
    top=$(changelog_top || true)
    [ -n "$top" ] || die "CHANGELOG.md 里找不到形如 '## <版本号>' 的顶部段落"
    [ "$p" = "$top" ] || die "版本不一致：common.props=<VersionPrefix>$p 但 CHANGELOG 顶部为 $top。二者必须在同一个 PR 里更新。"
    echo "ok: $p"
}

case "${1:-help}" in
prefix) prefix ;;
preview-tag) preview_tag ;;
release-tag) release_tag ;;
check-consistency) check_consistency ;;
release-notes) release_notes ;;
latest-release) latest_release ;;
changelog-top) changelog_top ;;
*) cat <<EOF
用法：$0 {prefix|preview-tag|release-tag|check-consistency|release-notes|latest-release|changelog-top}
EOF
    ;;
esac
