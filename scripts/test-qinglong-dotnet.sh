#!/usr/bin/env bash
# Exercise SDK detection without installing packages or running Bilibili tasks.
set -euo pipefail

repo_dir=$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)
source "$repo_dir/scripts/test-lib.sh"

fixture=$(mktemp -d)
trap 'rm -rf -- "$fixture"' EXIT
cat > "$fixture/dotnet" <<'EOF'
#!/usr/bin/env bash
if [[ "${1:-}" == --version ]]; then
    printf '%s' "$SDK_OUTPUT"
    exit "$SDK_EXIT"
fi
if [[ "${1:-}" == --list-sdks ]]; then
    printf '%s\n' '11.0.100 [/fake/sdk]'
    exit 0
fi
exit 2
EOF
chmod +x "$fixture/dotnet"
export PATH="$fixture:$PATH"
export SDK_OUTPUT SDK_EXIT

# Load actual function bodies without executing installation or cleanup code.
load_function() {
    local definition
    definition=$(awk -v name="$2" '
        $0 == name "() {" { capture = 1 }
        capture { print }
        capture && /^}$/ { exit }
    ' "$1")
    [[ -n "$definition" ]]
    eval "$definition"
}

cases=(
    '10|10.0.303'
    '8|8.0.131'
    '11|11.0.100'
    '10|10.0.303 [/usr/lib/dotnet/sdk]'
    $'10|8.0.131 [/usr/lib/dotnet/sdk]\n10.0.303 [/usr/lib/dotnet/sdk]'
    $'10|10.0.303 [/usr/lib/dotnet/sdk]\n8.0.131 [/usr/lib/dotnet/sdk]'
    $'8|8.0.131 [/fake/sdk]\n6.0.428 [/fake/sdk]'
    $'10|\n  8.0.131 [/fake/sdk]\r\n  10.0.303 [/path with spaces/sdk]\r\n\n'
    '10|10.0.100-preview.7.25380.108'
    '10|10.0.303+build.1'
    '10|10.0.100-rc.1+build.2'
    $'10|Welcome to .NET 10!\n10.0.303\nOther diagnostic text'
    $'10|10.0.303\n10.0.303'
    '|'
    '|No .NET SDKs were found.'
    '|10'
    '|10.0'
    '|Microsoft.NETCore.App 10.0.3 [/fake/shared]'
    '|10.0.303not-a-version'
    $'|A compatible .NET SDK was not found.\nRequested SDK version: 10.0.300'
)

scripts=(
    'platforms/qinglong/DefaultTasks/bili_task_base.sh'
    'platforms/qinglong/ray-dotnet-install.sh'
    'platforms/qinglong/extra.sh'
    'platforms/qinglong/DefaultTasks/bili_task_tryFix.sh'
)
for script in "${scripts[@]}"; do
    load_function "$repo_dir/$script" get_dotnet_major_version
    index=0
    for sample in "${cases[@]}"; do
        index=$((index + 1))
        expected=${sample%%|*}
        SDK_OUTPUT=${sample#*|}
        SDK_EXIT=0
        actual=$(get_dotnet_major_version dotnet || true)
        assert_eq "$script: fixture $index" "$expected" "$actual"
    done

    SDK_OUTPUT='10.0.303 [/fake/sdk]'
    SDK_EXIT=1
    assert_exit_nonzero "$script: failed SDK selection is rejected" get_dotnet_major_version dotnet
    SDK_OUTPUT=''
    SDK_EXIT=0
    assert_exit_nonzero "$script: missing dotnet is rejected" get_dotnet_major_version "$fixture/missing-dotnet"
    SDK_OUTPUT='10.0.303'
    assert_eq "$script: explicit legacy entry path" '10' "$(get_dotnet_major_version "$fixture/dotnet")"
done

base="$repo_dir/platforms/qinglong/DefaultTasks/bili_task_base.sh"
load_function "$base" get_dotnet_major_version
load_function "$base" check_dotnet
invocation=':'
say() { :; }
required_dotnet_major=10
installer="$repo_dir/platforms/qinglong/ray-dotnet-install.sh"
load_function "$installer" has_required_dotnet_version
for sample in "${cases[@]}"; do
    expected=${sample%%|*}
    SDK_OUTPUT=${sample#*|}
    SDK_EXIT=0
    for detector in check_dotnet has_required_dotnet_version; do
        actual_status=0
        "$detector" dotnet || actual_status=$?
        expected_status=1
        if [[ -n "$expected" && "$expected" -ge 10 ]]; then expected_status=0; fi
        assert_eq "$detector: required SDK for fixture ${sample%%|*}" "$expected_status" "$actual_status"
    done
done

# A listed newer SDK must not override a failing or incompatible selection.
SDK_OUTPUT='8.0.131'
SDK_EXIT=0
assert_exit_nonzero 'global.json selects .NET 8 despite newer installed SDKs' check_dotnet
SDK_OUTPUT='10.0.303'
SDK_EXIT=1
assert_exit_nonzero 'SDK selection failure is not masked by installed SDKs' check_dotnet
assert_exit_nonzero 'installer also rejects SDK selection failure' has_required_dotnet_version dotnet

finish
