#!/usr/bin/env bash
# release-notes.sh 的 fixture 测试：以模拟 PR 标题列表为输入，断言生成的
# release notes 与 CHANGELOG 段落。只测脚本 CLI 的外部行为。
#
# 本地/CI 通用：bash ./scripts/test-release-notes.sh
set -euo pipefail

repo_dir=$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)
sut="$repo_dir/scripts/release-notes.sh"

pass=0
fail=0

ok() {
    pass=$((pass + 1))
    echo "ok $pass - $1"
}

not_ok() {
    fail=$((fail + 1))
    echo "FAIL - $1" >&2
    echo "--- 期望 ---" >&2
    echo "$2" >&2
    echo "--- 实际 ---" >&2
    echo "$3" >&2
}

assert_eq() { # <msg> <expected> <actual>
    if [ "$2" = "$3" ]; then ok "$1"; else not_ok "$1" "$2" "$3"; fi
}

assert_exit_nonzero() { # <msg> <cmd...>
    local msg=$1; shift
    if "$@" >/dev/null 2>&1; then
        not_ok "$msg" "非 0 退出码" "0"
    else
        ok "$msg"
    fi
}

titles_file=$(mktemp)

# --- 场景 1：分类排序 + 不合规兜底 ---
cat > "$titles_file" <<'EOF'
维护[#1152]: 补齐 AI 协作者的仓库配置 (#1152)
随便一个不合规的标题 (#2000)
Fix[#1144]: Web 面板启动失败改为以非 0 退出码结束 (#1147)
Feature[#1100]: 新增某个配置项 (#1150)
**BREAKING[#1138]**: 部署平台目录统一迁移 (#1140)
EOF

expected='- **BREAKING[#1138]**: 部署平台目录统一迁移 (#1140)
- Feature[#1100]: 新增某个配置项 (#1150)
- Fix[#1144]: Web 面板启动失败改为以非 0 退出码结束 (#1147)
- 维护[#1152]: 补齐 AI 协作者的仓库配置 (#1152)
- 随便一个不合规的标题 (#2000)'

assert_eq "按 BREAKING/Feature/Fix/维护/其他 分类排序，原文保留" \
    "$expected" "$(bash "$sut" notes --from-file "$titles_file")"

# --- 场景 2：同类内保持合并顺序 ---
cat > "$titles_file" <<'EOF'
Fix[#1001]: 第一个修复 (#1001)
Fix[#1002]: 第二个修复 (#1002)
Fix[#1003]: 第三个修复 (#1003)
EOF

expected='- Fix[#1001]: 第一个修复 (#1001)
- Fix[#1002]: 第二个修复 (#1002)
- Fix[#1003]: 第三个修复 (#1003)'

assert_eq "同类内保持输入顺序" "$expected" "$(bash "$sut" notes --from-file "$titles_file")"

# --- 场景 3：空列表阻止发版 ---
: > "$titles_file"
assert_exit_nonzero "空 PR 列表报错退出" bash "$sut" notes --from-file "$titles_file"
assert_exit_nonzero "空 PR 列表时 section 同样报错" bash "$sut" section 4.0.8 --from-file "$titles_file"

# --- 场景 4：CHANGELOG 段落格式 ---
cat > "$titles_file" <<'EOF'
Fix[#1144]: 某个修复 (#1147)
EOF

expected='## 4.0.8

- Fix[#1144]: 某个修复 (#1147)'

assert_eq "section 输出 '## 版本号 + 空行 + 条目'" "$expected" "$(bash "$sut" section 4.0.8 --from-file "$titles_file")"

# --- 场景 5：全是不合规标题也能出（全进"其他"，不中断）---
cat > "$titles_file" <<'EOF'
Update something
bump deps
EOF

expected='- Update something
- bump deps'

assert_eq "全不合规标题原样收录" "$expected" "$(bash "$sut" notes --from-file "$titles_file")"

if [ "${CI:-}" = "true" ]; then rm -f "$titles_file" 2>/dev/null || true; fi

echo
echo "通过 $pass，失败 $fail"
[ "$fail" -eq 0 ]
