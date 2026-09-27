#!/usr/bin/env bash
# version-next.sh 的 fixture 测试：在临时 git 仓库里构造 tag 与提交序列，
# 断言算号结果。只测脚本 CLI 的外部行为（给定 git 历史 -> 版本号），不测实现细节。
#
# 本地/CI 通用：bash ./scripts/test-version-next.sh
set -euo pipefail

repo_dir=$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)
sut="$repo_dir/scripts/version-next.sh"

source "$repo_dir/scripts/test-lib.sh"

# 在临时目录造一个 git 仓库，全局变量 FIX 指向它
FIX=""
new_fixture() {
    FIX=$(mktemp -d)
    git -C "$FIX" init -q -b main
    git -C "$FIX" config user.email ci@example.com
    git -C "$FIX" config user.name ci
    git -C "$FIX" commit -q --allow-empty -m "init"
}

commit() { # <msg>：在 fixture 仓库里加一个空提交
    git -C "$FIX" commit -q --allow-empty -m "$1"
}

tag() { # <tag>：在 fixture 仓库当前位置打 tag
    git -C "$FIX" tag "$1"
}

run() { # <子命令...>：对 fixture 仓库执行被测脚本
    VERSION_REPO_DIR="$FIX" bash "$sut" "$@"
}

cleanup() {
    # 只在 CI 上主动删除 fixture 目录；本地跑时留着由操作系统清，
    # 某些本地沙箱会对 rm -rf 临时目录直接 SIGTERM，把测试进程一起带走。
    [ -n "$FIX" ] || return 0
    if [ "${CI:-}" = "true" ]; then rm -rf "$FIX" 2>/dev/null || true; fi
    FIX=""
}
trap cleanup EXIT

# --- 场景 1：无任何 tag（首次发版场景），版本线从 0.1.0 起步 ---
new_fixture
commit "a"; commit "b"
assert_eq "无 tag: alpha 基底为 0.1.0，N=提交数" "0.1.0-alpha.3" "$(run alpha)"
assert_eq "无 tag: stable patch 兜底 0.1.0" "0.1.0" "$(run stable patch)"
assert_eq "无 tag: stable minor 兜底 0.1.0" "0.1.0" "$(run stable minor)"
assert_eq "无 tag: stable major 兜底 0.1.0" "0.1.0" "$(run stable major)"
assert_eq "无 tag: latest 输出空" "" "$(run latest)"
cleanup

# --- 场景 2：稳定 tag 后的连续 alpha ---
new_fixture
commit "c1"; tag "4.0.7"
commit "c2"
assert_eq "首个 alpha: 基底=下一 patch，N=1" "4.0.8-alpha.1" "$(run alpha)"
commit "c3"; commit "c4"
assert_eq "第三个 alpha: N=3" "4.0.8-alpha.3" "$(run alpha)"
assert_eq "latest 为最新稳定 tag" "4.0.7" "$(run latest)"
cleanup

# --- 场景 3：stable 按级别 bump ---
new_fixture
commit "c1"; tag "4.0.7"
commit "c2"
assert_eq "patch bump" "4.0.8" "$(run stable patch)"
assert_eq "minor bump" "4.1.0" "$(run stable minor)"
assert_eq "major bump" "5.0.0" "$(run stable major)"
assert_exit_nonzero "未知级别报错" run stable banana
cleanup

# --- 场景 4：发稳定版后 alpha 序号复位、基底前移 ---
new_fixture
commit "c1"; tag "4.0.7"
commit "c2"; tag "4.0.8"   # 模拟发了 4.0.8
commit "c3"
assert_eq "发版后首个 alpha: 基底=4.0.9，N 复位为 1" "4.0.9-alpha.1" "$(run alpha)"
cleanup

new_fixture
commit "c1"; tag "4.1.0"   # 跨 minor 发版之后
commit "c2"
assert_eq "跨 minor 后: alpha 基底为其下一 patch" "4.1.1-alpha.1" "$(run alpha)"
cleanup

new_fixture
commit "c1"; tag "5.0.0"   # 跨 major 发版之后
commit "c2"
assert_eq "跨 major 后: alpha 基底为其下一 patch" "5.0.1-alpha.1" "$(run alpha)"
cleanup

# --- 场景 5：异类 tag 不算稳定 tag ---
new_fixture
commit "c1"; tag "v4.0.0.6"   # 历史 v 前缀异类
commit "c2"; tag "4.0.8-alpha.1" # alpha tag 不是稳定版
commit "c3"; tag "3.8.2"
commit "c4"
assert_eq "v 前缀与 alpha tag 被排除" "3.8.2" "$(run latest)"
assert_eq "alpha 基于最新纯数字 tag" "3.8.3-alpha.1" "$(run alpha)"
cleanup

# --- 场景 6：守卫 ---
new_fixture
commit "c1"; tag "4.0.7"
assert_exit_nonzero "HEAD 无新提交时 alpha 报错" run alpha
cleanup

finish
