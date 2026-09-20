#!/usr/bin/env bash
# 把这个 PR 合并后 CI 会算出的版本号，播报成一条 PR 评论。
#
# 为什么用评论而不是写回文件：镜像 tag / 预览版本号是由 common.props 的
# <VersionPrefix> 加 git tag 推导出来的（见 scripts/version.sh），把它写死进
# common.props 或 CHANGELOG.md 反而会在合并后立刻过期。CLR 的数字 AssemblyVersion
# 也装不下 -alpha.N 这类后缀，所以文件里本来就不可能存在一个"和镜像 tag 一致"的号。
set -euo pipefail

repo_dir=$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)
version_sh="$repo_dir/scripts/version.sh"

# 用来定位自己发过的那条评论，避免每次 push 都叠一条新的
marker='<!-- ci-planned-version -->'

hint='想改这个号：编辑 `common.props` 的 `<VersionPrefix>`，并在 `CHANGELOG.md` 顶部开一个同号段落，二者必须一致（CI 会校验）。'

# 输出评论正文；base 不是 main/develop 时输出空串
body() {
    local branch p latest planned
    branch=${1:-}
    [ -n "$branch" ] || branch=$(git -C "$repo_dir" rev-parse --abbrev-ref HEAD)
    p=$("$version_sh" prefix)

    case "$branch" in
    develop)
        planned=$("$version_sh" preview-tag 2>/dev/null)
        if [ "$planned" = "SKIP" ]; then
            latest=$("$version_sh" latest-release)
            cat <<EOF
$marker
### 计划版本

⚠️ 合并到 \`develop\` 后**不会**构建预览镜像：\`<VersionPrefix>\` 还是 $p，没有推进过最新正式版 \`$latest\`。

$hint
EOF
        else
            cat <<EOF
$marker
### 计划版本

合并到 \`develop\` 后推送预览镜像：**\`$planned\`**（同时更新镜像的 \`:develop\` tag；\`:latest\` 只由 \`main\` 的发版推动，预览不会碰它）。

容器日志里显示的版本和这个 tag 一致。

$hint
EOF
        fi
        ;;
    main)
        planned=$("$version_sh" release-tag 2>/dev/null)
        if [ "$planned" = "SKIP" ]; then
            cat <<EOF
$marker
### 计划版本

⚠️ 合并到 \`main\` 后**不会**发版：tag \`$p\` 已经存在。

要发新版本，需要先推进版本号。$hint
EOF
        else
            cat <<EOF
$marker
### 计划版本

合并到 \`main\` 后正式发版：**\`$planned\`**

- 打 git tag \`$planned\`，并创建同名 GitHub Release（附带各运行时 zip 包，正文取 CHANGELOG 顶部段落）
- 推送镜像 **\`$planned\`** 和 **\`:latest\`**（DockerHub + GHCR）

这是最终版本号，不会再带 \`-alpha\` 后缀。
EOF
        fi
        ;;
    *) ;;
    esac
}

# 创建或更新自己那条播报评论
post() {
    local repo pr text existing
    repo=${GITHUB_REPOSITORY:?需要 GITHUB_REPOSITORY，本地验证请用 print}
    pr=${PR_NUMBER:?需要 PR_NUMBER，本地验证请用 print}
    text=$(body "${1:-}")
    if [ -z "$text" ]; then
        echo "base 分支不是 main/develop，跳过版本播报"
        return 0
    fi
    existing=$(
        gh api "repos/$repo/issues/$pr/comments" --paginate \
            --jq ".[] | select(.body | contains(\"$marker\")) | .id" | tail -1 || true
    )
    if [ -n "$existing" ]; then
        gh api -X PATCH "repos/$repo/issues/comments/$existing" -f body="$text" >/dev/null
        echo "已更新计划版本播报（评论 $existing）"
    else
        gh api -X POST "repos/$repo/issues/$pr/comments" -f body="$text" >/dev/null
        echo "已发布计划版本播报"
    fi
}

case "${1:-help}" in
print) body "${2:-}" ;;
post) post "${2:-}" ;;
*)
    cat <<EOF
用法：$0 {print|post} [base分支]
  print  只打印评论正文，便于本地查看
  post   通过 gh 创建/更新 PR 上自己那条播报评论（需要 GITHUB_REPOSITORY、PR_NUMBER、GH_TOKEN）
EOF
    ;;
esac
