#!/usr/bin/env bash
# update-changelog.sh 的 fixture 测试：验证段落插入、同版本重跑替换、空输入守卫。
# 本地/CI 通用：bash ./scripts/test-update-changelog.sh
set -euo pipefail

repo_dir=$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)
sut="$repo_dir/scripts/update-changelog.sh"

source "$repo_dir/scripts/test-lib.sh"

FIX=$(mktemp -d)
titles_file=$(mktemp)

new_changelog() { # 造一个带两个历史段落的 CHANGELOG
    cat > "$FIX/CHANGELOG.md" <<'EOF'
## 4.0.7
- Fix[#1144]: 历史修复 A (#1147)
## 4.0.6
- 维护[#1133]: 历史维护 B (#1135)
EOF
}

run() {
    VERSION_REPO_DIR="$FIX" bash "$sut" "$@"
}

# --- 场景 1：新段落插入顶部，历史段落原样保留 ---
new_changelog
cat > "$titles_file" <<'EOF'
Fix[#1150]: 新修复 (#1151)
EOF

expected='## 4.0.8
- Fix[#1150]: 新修复 (#1151)
## 4.0.7
- Fix[#1144]: 历史修复 A (#1147)
## 4.0.6
- 维护[#1133]: 历史维护 B (#1135)'

run 4.0.8 --from-file "$titles_file" > /dev/null
assert_eq "新版本段落插入顶部" "$expected" "$(cat "$FIX/CHANGELOG.md")"

# --- 场景 2：顶部已是同版本（重跑），替换而非重复插入 ---
new_changelog
cat > "$FIX/CHANGELOG.md" <<'EOF'
## 4.0.8
- Fix[#1150]: 新修复 (#1151)
## 4.0.7
- Fix[#1144]: 历史修复 A (#1147)
EOF
cat > "$titles_file" <<'EOF'
Fix[#1150]: 新修复（重跑改了标题） (#1151)
EOF

expected='## 4.0.8
- Fix[#1150]: 新修复（重跑改了标题） (#1151)
## 4.0.7
- Fix[#1144]: 历史修复 A (#1147)'

run 4.0.8 --from-file "$titles_file" > /dev/null
assert_eq "同版本重跑：替换顶部段落" "$expected" "$(cat "$FIX/CHANGELOG.md")"
assert_eq "同版本重跑：段落不重复" "1" "$(grep -c '^## 4.0.8$' "$FIX/CHANGELOG.md")"

# --- 场景 3：空标题列表报错且 CHANGELOG 不被改动 ---
new_changelog
: > "$titles_file"
before=$(cat "$FIX/CHANGELOG.md")
assert_exit_nonzero "空标题列表报错" run 4.0.9 --from-file "$titles_file"
assert_eq "失败后 CHANGELOG 未被改动" "$before" "$(cat "$FIX/CHANGELOG.md")"

# 与 test-version-next.sh 同一策略：只在 CI 上删除临时文件
if [ "${CI:-}" = "true" ]; then rm -rf "$FIX" "$titles_file" 2>/dev/null || true; fi

finish
