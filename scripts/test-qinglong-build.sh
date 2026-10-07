#!/usr/bin/env bash
# Run only in a disposable SDK container without networking or production mounts.
set -euo pipefail
test "${ISOLATED_QA:-}" = 1
source_root=${SOURCE_ROOT:-/src}
fixture=$(mktemp -d /tmp/bili-build-test.XXXXXXXX)
jobs=()
stage=setup
cleanup() {
    local result=$?
    if test "$result" != 0; then
        printf 'Synthetic stage failed: %s\n' "$stage" >&2
        for log in "$fixture/"*.log; do test ! -f "$log" || tail -n 8 "$log" >&2; done
    fi
    for job in "${jobs[@]}"; do kill "$job" 2>/dev/null || true; done
    [[ "$fixture" == /tmp/bili-build-test.* && ! -L "$fixture" ]] || return 1
    rm -rf -- "$fixture"
}
trap cleanup EXIT
export QL_DIR="$fixture/ql with spaces" BILI_MODE=dotnet
export DOTNET_CLI_TELEMETRY_OPTOUT=1 DOTNET_CLI_WORKLOAD_UPDATE_NOTIFY_DISABLE=true
repo="$QL_DIR/data/repo/synthetic owner_BiliBiliToolPro"
console="$repo/src/Ray.BiliBiliTool.Console"
scripts="$repo/platforms/qinglong/DefaultTasks"
mkdir -p "$console" "$repo/src/Synthetic.Contracts" "$scripts" "$QL_DIR/shell"
cp "$source_root/platforms/qinglong/DefaultTasks/"*.sh "$scripts/"
: > /root/.bashrc
: > "$QL_DIR/shell/env.sh"
printf '%s\n' 'dir_repo="$QL_DIR/data/repo"' > "$QL_DIR/shell/share.sh"
printf '%s\n' '<configuration><packageSources><clear /></packageSources></configuration>' > "$repo/NuGet.Config"
cat > "$repo/src/Synthetic.Contracts/Synthetic.Contracts.csproj" <<'EOF'
<Project Sdk="Microsoft.NET.Sdk"><PropertyGroup><TargetFramework>net10.0</TargetFramework></PropertyGroup></Project>
EOF
printf '%s\n' 'namespace Synthetic; public static class Value { public static string Text => "synthetic-only"; }' > "$repo/src/Synthetic.Contracts/Value.cs"
cat > "$console/Ray.BiliBiliTool.Console.csproj" <<'EOF'
<Project Sdk="Microsoft.NET.Sdk">
 <PropertyGroup><OutputType>Exe</OutputType><TargetFramework>net10.0</TargetFramework></PropertyGroup>
 <ItemGroup><ProjectReference Include="../Synthetic.Contracts/Synthetic.Contracts.csproj" /></ItemGroup>
 <Target Name="SyntheticGate" BeforeTargets="_CopyFilesMarkedCopyLocal" Condition="'$(GATE_SH)' != ''">
  <Exec Command="bash &quot;$(GATE_SH)&quot;" EnvironmentVariables="ARTIFACTS_ROOT=$(ArtifactsPath)" />
 </Target>
</Project>
EOF
cat > "$console/Program.cs" <<'EOF'
System.Console.WriteLine("task="+System.Environment.GetEnvironmentVariable("Ray_RunTasks"));
System.Console.WriteLine("platform="+System.Environment.GetEnvironmentVariable("Ray_PlatformType"));
System.Console.WriteLine("cwd="+System.Environment.CurrentDirectory);
System.Console.WriteLine("config="+System.IO.File.ReadAllText("appsettings.json"));
System.Console.WriteLine("args="+string.Join("|",args));
System.Console.WriteLine("dependency="+Synthetic.Value.Text);
System.Console.WriteLine("appbase="+System.AppContext.BaseDirectory);
if (System.Environment.GetEnvironmentVariable("QA_HOLD") is {} ready) {
 System.IO.File.WriteAllText(ready,"ready");
 await System.Threading.Tasks.Task.Delay(System.TimeSpan.FromSeconds(30));
}
return int.TryParse(System.Environment.GetEnvironmentVariable("QA_EXIT"),out int code)?code:0;
EOF
printf 'synthetic-config' > "$console/appsettings.json"
# Populate legacy bin/obj first to verify that opting into artifacts excludes them.
(cd "$console"; dotnet build --nologo -p:NuGetAudit=false > "$fixture/legacy.log" 2>&1)
test -f "$repo/src/Synthetic.Contracts/bin/Debug/net10.0/Synthetic.Contracts.pdb"
gate="$fixture/gate.sh"
cat > "$gate" <<'EOF'
#!/bin/bash
set -eu
printf '%s' "$ARTIFACTS_ROOT" > "$GATE_READY.root"
touch "$GATE_READY"
for i in $(seq 1 400);do test ! -e "$GATE_CONTINUE" || exit 0;sleep .05;done
exit 92
EOF
wait_file() {
    for i in $(seq 1 600); do test ! -f "$1" || return 0; sleep .05; done
    echo 'Synthetic stage timed out' >&2; return 1
}
check_output() {
    grep -Fx "task=$2" "$1" >/dev/null
    grep -Fx 'platform=QingLong' "$1" >/dev/null
    grep -Fx "cwd=$console" "$1" >/dev/null
    grep -Fx 'config=synthetic-config' "$1" >/dev/null
    grep -Fx 'args=--ENVIRONMENT=Production' "$1" >/dev/null
    grep -Fx 'dependency=synthetic-only' "$1" >/dev/null
    local appbase
    appbase=$(sed -n 's/^appbase=//p' "$1")
    [[ "$appbase" == /tmp/bilitool-qinglong.*/bin/Ray.BiliBiliTool.Console/* ]]
    test ! -e "${appbase%%/bin/*}"
}
check_no_artifacts() {
    # Cancellation reaches each process separately; wait for the child's exit trap.
    for i in $(seq 1 40); do
        test -n "$(find /tmp -maxdepth 1 -name 'bilitool-qinglong.*' -print -quit)" || return 0
        sleep .05
    done
    return 1
}
cases=0
stage=control
(cd "$scripts"; bash bili_task_daily.sh) > "$fixture/control.log" 2>&1
check_output "$fixture/control.log" Daily;check_no_artifacts
cases=$((cases+1))

# Cleanup runs at the exact stage that reproduced MSB3030 in the old script.
stage=cleanup-conflict
(cd "$scripts"; GATE_SH="$gate" GATE_READY="$fixture/ready" GATE_CONTINUE="$fixture/continue" bash bili_task_daily.sh) > "$fixture/clean.log" 2>&1 &
job=$!;jobs+=("$job")
wait_file "$fixture/ready"
artifacts=$(cat "$fixture/ready.root")
[[ "$artifacts" == /tmp/bilitool-qinglong.* ]]
test -f "$artifacts/bin/Synthetic.Contracts/debug/Synthetic.Contracts.pdb"
(cd "$scripts"; bash bili_task_tryFix.sh) > "$fixture/cleanup.log" 2>&1
test -f "$artifacts/bin/Synthetic.Contracts/debug/Synthetic.Contracts.pdb"
touch "$fixture/continue"
wait "$job";check_output "$fixture/clean.log" Daily;check_no_artifacts
cases=$((cases+1))

# Both tasks must reach the build stage together and retain different outputs.
stage=concurrent-tasks
for task in Daily LiveFansMedal;do
 (cd "$scripts"; export GATE_SH="$gate" GATE_READY="$fixture/$task.ready" GATE_CONTINUE="$fixture/$task.continue"; . ./bili_task_base.sh;run_task "$task") > "$fixture/$task.log" 2>&1 &
 jobs+=("$!")
done
wait_file "$fixture/Daily.ready";wait_file "$fixture/LiveFansMedal.ready"
test "$(cat "$fixture/Daily.ready.root")" != "$(cat "$fixture/LiveFansMedal.ready.root")"
touch "$fixture/Daily.continue" "$fixture/LiveFansMedal.continue"
for job in "${jobs[@]: -2}";do wait "$job";done
for task in Daily LiveFansMedal;do check_output "$fixture/$task.log" "$task";done
check_no_artifacts;cases=$((cases+1))

status=0
stage=failure-exit
(cd "$scripts"; QA_EXIT=7 bash bili_task_daily.sh) > "$fixture/failure.log" 2>&1 || status=$?
test "$status" = 7;check_output "$fixture/failure.log" Daily;check_no_artifacts
cases=$((cases+1))

# A failed temporary-directory allocation must stop before launching dotnet run.
stage=allocation-failure
mkdir "$fixture/mock"
printf '%s\n' '#!/bin/bash' 'exit 42' > "$fixture/mock/mktemp"
chmod +x "$fixture/mock/mktemp"
status=0
(cd "$scripts"; PATH="$fixture/mock:$PATH" bash bili_task_daily.sh) > "$fixture/allocation.log" 2>&1 || status=$?
test "$status" != 0
test -z "$(sed -n 's/^appbase=//p' "$fixture/allocation.log")"
check_no_artifacts;cases=$((cases+1))

# Preserve the legacy self-contained execution path and working directory.
stage=self-contained
mkdir -p "$repo/bin"
printf 'synthetic-version\n' > "$repo/bin/tag.txt"
cat > "$repo/bin/Ray.BiliBiliTool.Console" <<'EOF'
#!/bin/bash
printf 'binary-task=%s\nbinary-cwd=%s\nbinary-args=%s\n' "$Ray_RunTasks" "$PWD" "$*"
EOF
chmod +x "$repo/bin/Ray.BiliBiliTool.Console"
(cd "$scripts"; BILI_MODE=bilitool bash bili_task_daily.sh) > "$fixture/binary.log" 2>&1
grep -Fx 'binary-task=Daily' "$fixture/binary.log" >/dev/null
grep -Fx "binary-cwd=$console" "$fixture/binary.log" >/dev/null
grep -Fx 'binary-args=--ENVIRONMENT=Production' "$fixture/binary.log" >/dev/null
check_no_artifacts;cases=$((cases+1))

# Simulate a panel terminating the complete task process group.
stage=cancellation
setsid bash -c 'echo $$ > "$1";cd "$2";export QA_HOLD="$3";exec bash bili_task_daily.sh' qa "$fixture/pid" "$scripts" "$fixture/holding" > "$fixture/cancel.log" 2>&1 &
job=$!;jobs+=("$job")
wait_file "$fixture/holding"
kill -TERM -- "-$(cat "$fixture/pid")"
status=0;wait "$job" || status=$?
test "$status" != 0
check_no_artifacts;cases=$((cases+1))
test "$(cat "$console/appsettings.json")" = synthetic-config
printf '{"real_sdk_cases":%s,"passed":%s,"failed":0,"legacy_outputs_excluded":true,"cleanup_conflict_fixed":true,"parallel_tasks_preserved":true,"settings_preserved":true,"temporary_artifacts_removed":true,"network":"none","bilibili_requests":0,"notification_requests":0}\n' "$cases" "$cases"
