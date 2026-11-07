#!/usr/bin/env bash
# Fixture tests for the Alpine .NET 10 package check in the panel scripts.
# The scripts run top-level code when executed, so each function under test is
# extracted by name and evaluated in isolation.
#
# Local/CI: bash ./scripts/test-alpine-dotnet-version.sh
set -euo pipefail

repo_dir=$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)

source "$repo_dir/scripts/test-lib.sh"

scripts=(
    platforms/qinglong/DefaultTasks/bili_task_base.sh
    platforms/daidai/DefaultTasks/bili_task_base.sh
    platforms/qinglong/ray-dotnet-install.sh
)

extract_fn() { # <file> <function name>: print the function definition
    awk -v name="$2" '
        $0 ~ "^" name "\\(\\) *\\{" { on = 1 }
        on { print }
        on && /^}/ { exit }
    ' "$1"
}

check_version() { # <script> <VERSION_ID>: print yes/no
    local def
    def=$(extract_fn "$repo_dir/$1" alpine_has_dotnet10_package)
    if [ -z "$def" ]; then
        echo "missing"
        return
    fi
    if (eval "$def" && alpine_has_dotnet10_package "$2"); then echo yes; else echo no; fi
}

for script in "${scripts[@]}"; do
    for v in 3.23 3.24 3.23.0 3.23.3 3.24.0 3.24.2 3.30.1 4.0.0 3.25.0_alpha20261001; do
        assert_eq "$script: Alpine $v provides dotnet10-sdk" "yes" "$(check_version "$script" "$v")"
    done
    for v in 3.22.2 3.21.0 3.9.1 2.99.0 "" unknown 3 edge; do
        assert_eq "$script: Alpine '$v' is rejected" "no" "$(check_version "$script" "$v")"
    done
    if grep -q '!= 3\.23\*' "$repo_dir/$script"; then
        not_ok "$script: no hard-coded 3.23* match left" "no match" "$(grep -n '!= 3\.23\*' "$repo_dir/$script")"
    else
        ok "$script: no hard-coded 3.23* match left"
    fi
done

# Behavior: ray-dotnet-install.sh installs from apk on Alpine 3.24.2 (#1248).
install_fn=$(extract_fn "$repo_dir/platforms/qinglong/ray-dotnet-install.sh" install_for_alpine)
helper_fn=$(extract_fn "$repo_dir/platforms/qinglong/ray-dotnet-install.sh" alpine_has_dotnet10_package)
out=$(
    apk() { echo "apk $*"; }
    configure_alpine_dotnet_path() { echo "configured"; }
    eval "$helper_fn"
    eval "$install_fn"
    VERSION_ID=3.24.2
    install_for_alpine 2>&1 && echo "status=0" || echo "status=$?"
)
assert_contains "install_for_alpine on 3.24.2 runs apk add dotnet10-sdk" "apk add --no-cache dotnet10-sdk" "$out"
assert_contains "install_for_alpine on 3.24.2 succeeds" "status=0" "$out"

finish
