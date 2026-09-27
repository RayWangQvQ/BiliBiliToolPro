#!/usr/bin/env bash
# fixture 测试的共用断言框架，被 test-*.sh source，不单独执行。
# 约定：测试脚本末尾调用 finish 汇总并以失败数决定退出码。

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

assert_contains() { # <msg> <期望子串> <actual>
    case "$3" in
    *"$2"*) ok "$1" ;;
    *) not_ok "$1" "包含: $2" "$3" ;;
    esac
}

assert_exit_nonzero() { # <msg> <cmd...>
    local msg=$1
    shift
    if "$@" >/dev/null 2>&1; then
        not_ok "$msg" "非 0 退出码" "0"
    else
        ok "$msg"
    fi
}

finish() {
    echo
    echo "通过 $pass，失败 $fail"
    [ "$fail" -eq 0 ]
}
