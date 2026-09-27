#!/usr/bin/env bash
# 新发布策略（ADR-0002）的 release notes / CHANGELOG 段落生成器。
# 输入是自上个稳定 tag 以来合并的 PR 标题（squash 提交的标题行），
# 按类型前缀分类排序输出；PR 标题不做强制校验，不合规的原样收录进末尾，绝不中断发版。
#
#   notes [--since <tag>] [--from-file <f>]   -> "- <标题>" 条目列表
#   section <版本> [--since <tag>] [--from-file <f>] -> "## <版本>" + 空行 + 条目
#
# 默认 --since 为最新稳定 tag（version-next.sh latest）；--from-file 是测试 seam。
set -euo pipefail

repo_dir=${VERSION_REPO_DIR:-$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)}

die() {
    echo "$1" >&2
    exit 1
}

# 分类优先级：BREAKING > Feature > Fix > 维护 > 其他。
# 判定前剥掉前导 **（历史 BREAKING 条目用粗体包裹），但输出保留原文。
rank_of() {
    local t=$1
    t=$(sed -E 's/^\*+//; s/^[[:space:]]+//' <<< "$t")
    case "$t" in
    BREAKING* | breaking*) echo 0 ;;
    Feature* | feature* | feat*) echo 1 ;;
    Fix* | fix*) echo 2 ;;
    维护*) echo 3 ;;
    *) echo 4 ;;
    esac
}

# 稳定排序：按 rank 分类，同类内保持输入（合并）顺序
sort_titles() {
    local i=0 t
    while IFS= read -r t; do
        [ -n "$t" ] || continue
        echo "$(rank_of "$t") $i $t"
        i=$((i + 1))
    done | sort -s -n -k1,1 -k2,2 | cut -d' ' -f3-
}

read_titles() { # <since> <from_file>
    if [ -n "$2" ]; then
        cat "$2"
    elif [ -n "$1" ]; then
        git -C "$repo_dir" log --pretty=%s "$1..HEAD"
    else
        git -C "$repo_dir" log --pretty=%s
    fi
}

latest_stable() {
    git -C "$repo_dir" tag -l | grep -E '^[0-9]+\.[0-9]+\.[0-9]+$' | sort -V | tail -1 || true
}

entries() { # <since> <from_file>：条目列表；空列表报错
    local since=$1 from_file=$2 titles
    titles=$(read_titles "$since" "$from_file")
    [ -n "${titles// /}" ] || die "自 ${since:-仓库起点} 以来没有合并的 PR，拒绝发空版本"
    sort_titles <<< "$titles" | sed 's/^/- /'
}

cmd=${1:-help}
shift || true
case "$cmd" in
notes | section)
    version=""
    if [ "$cmd" = section ]; then
        version=${1:-}
        [ -n "$version" ] || die "section 需要版本号参数"
        shift
    fi
    since=""
    from_file=""
    while [ $# -gt 0 ]; do
        case "$1" in
        --since)
            since=$2
            shift 2
            ;;
        --from-file)
            from_file=$2
            shift 2
            ;;
        *) die "未知参数: $1" ;;
        esac
    done
    [ -n "$from_file" ] || since=${since:-$(latest_stable)}
    body=$(entries "$since" "$from_file")
    if [ "$cmd" = section ]; then
        printf '## %s\n\n%s\n' "$version" "$body"
    else
        echo "$body"
    fi
    ;;
*)
    cat <<EOF
用法：$0 {notes|section <版本>} [--since <tag>] [--from-file <文件>]
  notes    输出分类排序后的 "- <标题>" 条目
  section  输出 CHANGELOG 段落（## <版本> + 条目）
  --since     从该 tag 到 HEAD 的提交标题（缺省：最新稳定 tag）
  --from-file 从文件读标题列表（测试 seam，优先级高于 --since）
环境变量 VERSION_REPO_DIR 可指定目标 git 仓库。
EOF
    ;;
esac
