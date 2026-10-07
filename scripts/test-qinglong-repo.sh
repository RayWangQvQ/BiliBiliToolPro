#!/usr/bin/env bash
# Run in a disposable, network-disabled container without production mounts.
set -euo pipefail
test "${ISOLATED_QA:-}" = 1
source_root=${SOURCE_ROOT:-/src}
fixture=$(mktemp -d /tmp/bili-repo-test.XXXXXX)
trap 'rm -rf -- "$fixture"' EXIT
mkdir -p "$fixture/mock"
: > /root/.bashrc
cat > "$fixture/mock/dotnet" <<'EOF'
#!/usr/bin/env bash
case "${1:-}" in
 --version) printf '10.0.303\n' ;;
 --info) exit 0 ;;
 run) printf '%s|%s\n' "$PWD" "${Ray_RunTasks:-}" >> "$CALL_FILE" ;;
 *) exit 92 ;;
esac
EOF
for command in curl apt-get apk dpkg; do
 printf '%s\n' '#!/usr/bin/env bash' 'echo blocked >> "$NETWORK_FILE"' 'exit 91' > "$fixture/mock/$command"
done
chmod +x "$fixture/mock/"*
export PATH="$fixture/mock:$PATH"
export CALL_FILE NETWORK_FILE RESULT_FILE SCRIPT_PATH QL_DIR CUSTOM_REPO
passed=0;failed=0;unsafe=0;cases=0

seed_repo() {
 mkdir -p "$1/src/Ray.BiliBiliTool.Console" "$1/platforms" "$1/bin" "$1/obj"
 : > "$1/src/Ray.BiliBiliTool.Console/Ray.BiliBiliTool.Console.csproj"
 : > "$1/bin/cache"
 : > "$1/obj/cache"
 cp -R "$source_root/platforms/qinglong" "$1/platforms/"
}

scenarios=(official mixed-case main-branch custom-branch explicit-branch missing-branch fork spaces legacy custom-root default-env missing empty invalid invalid-neighbor ambiguous own-repo own-symlink duplicate-alias nested-only outside-symlink console-symlink bin-symlink)
for entry in base tryFix; do
 for scenario in "${scenarios[@]}"; do
  cases=$((cases+1))
  case_dir="$fixture/case-$cases"
  QL_DIR="$case_dir/ql";CUSTOM_REPO=''
  repo_parent="$QL_DIR/data/repo";expected='';allowed=1
  test "$scenario" != spaces || QL_DIR="$case_dir/ql with spaces"
  test "$scenario" != spaces || repo_parent="$QL_DIR/data/repo"
  test "$scenario" != legacy || repo_parent="$QL_DIR/repo"
  test "$scenario" != custom-root || repo_parent="$case_dir/custom repos"
  test "$scenario" != custom-root || CUSTOM_REPO="$repo_parent"
  mkdir -p "$repo_parent" "$QL_DIR/shell" "$QL_DIR/data/scripts" "$case_dir/unrelated/bin" /root/bin /root/obj
  printf '%s\n' 'if [[ -n "${CUSTOM_REPO:-}" ]]; then dir_repo="$CUSTOM_REPO"; fi' > "$QL_DIR/shell/env.sh"
  printf '%s\n' 'dir_repo="${CUSTOM_REPO:-$QL_DIR/data/repo}"' > "$QL_DIR/shell/share.sh"
  : > "$case_dir/unrelated/bin/keep"
  : > /root/bin/keep
  : > /root/obj/keep
  CALL_FILE="$case_dir/calls";NETWORK_FILE="$case_dir/network";RESULT_FILE="$case_dir/result"
  : > "$CALL_FILE";: > "$NETWORK_FILE"
  repo_name=raywangqvq_bilibilitoolpro
  case "$scenario" in
   mixed-case) repo_name=RayWangQvQ_BiliBiliToolPro ;;
   main-branch) repo_name=raywangqvq_bilibilitoolpro_main ;;
   custom-branch) repo_name=raywangqvq_bilibilitoolpro_develop ;;
   explicit-branch) repo_name=raywangqvq_bilibilitoolpro_develop ;;
   fork|spaces|own-repo|own-symlink|invalid-neighbor) repo_name='another-owner_BiliBiliToolPro_fork' ;;
  esac
  expected="$repo_parent/$repo_name"
  case "$scenario" in
   missing) rmdir "$repo_parent";allowed=0;expected='' ;;
   empty) allowed=0;expected='' ;;
   invalid) mkdir -p "$expected";allowed=0;expected='' ;;
   *) seed_repo "$expected" ;;
  esac
  case "$scenario" in
   ambiguous) seed_repo "$repo_parent/another-owner_BiliBiliToolPro";allowed=0 ;;
   explicit-branch) seed_repo "$repo_parent/raywangqvq_bilibilitoolpro" ;;
   missing-branch) allowed=0 ;;
   own-repo|own-symlink) seed_repo "$repo_parent/raywangqvq_bilibilitoolpro" ;;
   duplicate-alias) ln -s "$expected" "$repo_parent/alias" ;;
   invalid-neighbor) mkdir -p "$repo_parent/raywangqvq_bilibilitoolpro/bin";: > "$repo_parent/raywangqvq_bilibilitoolpro/bin/keep" ;;
   nested-only) mkdir -p "$repo_parent/archive";mv "$expected" "$repo_parent/archive/";allowed=0 ;;
   outside-symlink)
    mv "$expected" "$case_dir/outside-repo"
    ln -s "$case_dir/outside-repo" "$expected"
    allowed=0 ;;
   console-symlink)
    mv "$expected/src/Ray.BiliBiliTool.Console" "$case_dir/outside-console"
    ln -s "$case_dir/outside-console" "$expected/src/Ray.BiliBiliTool.Console"
    allowed=0 ;;
   bin-symlink)
    rm -rf -- "$expected/bin"
    ln -s "$case_dir/unrelated/bin" "$expected/bin"
    test "$entry" != base || allowed=0 ;;
  esac
  script="bili_task_$entry.sh"
  SCRIPT_PATH="$QL_DIR/data/scripts/$script"
  cp "$source_root/platforms/qinglong/DefaultTasks/$script" "$SCRIPT_PATH"
  if test "$scenario" = explicit-branch || test "$scenario" = missing-branch;then
   # Preserve the existing inline branch setting used by copied Qinglong tasks.
   sed -i 's/^bili_branch="".*/bili_branch="_develop"/' "$SCRIPT_PATH"
  fi
  if test "$scenario" = own-repo; then SCRIPT_PATH="$expected/platforms/qinglong/DefaultTasks/$script";fi
  if test "$scenario" = own-symlink; then
   rm -- "$SCRIPT_PATH"
   ln -s "$expected/platforms/qinglong/DefaultTasks/$script" "$SCRIPT_PATH"
  fi
  status=0
  if test "$scenario" = default-env;then test ! -e /ql;ln -s "$QL_DIR" /ql;fi
  (cd "$case_dir"; unset dir_repo;test "$scenario" != default-env || unset QL_DIR; bash -c '. "$SCRIPT_PATH"; printf "%s" "$qinglong_bili_repo_dir" > "$RESULT_FILE"') > "$case_dir/output" 2>&1 || status=$?
  if test "$scenario" = default-env;then test -L /ql;rm -- /ql;fi
  good=1
  if test "$allowed" = 1; then
   test "$status" = 0 && test -f "$RESULT_FILE" && test "$(cat "$RESULT_FILE")" = "$expected" || good=0
   if test "$entry" = tryFix; then test ! -e "$expected/obj/cache" || good=0;fi
  else
   test "$status" != 0 && test ! -e "$RESULT_FILE" || good=0
  fi
  if test ! -f /root/bin/keep || test ! -f /root/obj/keep || test ! -f "$case_dir/unrelated/bin/keep"; then
   unsafe=$((unsafe+1));good=0
  fi
  test ! -s "$CALL_FILE" && test ! -s "$NETWORK_FILE" || good=0
  if test "$scenario" = invalid-neighbor;then test -f "$repo_parent/raywangqvq_bilibilitoolpro/bin/keep" || good=0;fi
  if test "$good" = 1;then passed=$((passed+1));else failed=$((failed+1));printf 'FAIL %s/%s\n' "$entry" "$scenario" >&2;fi
 done
done

# Exercise the unchanged task wrapper and both execution modes with real paths.
for mode in dotnet bilitool; do
 for layout in regular spaces;do
  cases=$((cases+1));case_dir="$fixture/case-$cases"
  QL_DIR="$case_dir/ql";CUSTOM_REPO=''
  test "$layout" != spaces || QL_DIR="$case_dir/ql with spaces"
  expected="$QL_DIR/data/repo/fork-owner_BiliBiliToolPro"
  seed_repo "$expected"
  mkdir -p "$QL_DIR/shell" "$QL_DIR/data/scripts"
  : > "$QL_DIR/shell/env.sh"
  CALL_FILE="$case_dir/calls";NETWORK_FILE="$case_dir/network";RESULT_FILE="$case_dir/result"
  : > "$CALL_FILE";: > "$NETWORK_FILE"
  printf '1.0\n' > "$expected/bin/tag.txt"
  cat > "$expected/bin/Ray.BiliBiliTool.Console" <<'EOF'
#!/usr/bin/env bash
printf '%s|%s\n' "$PWD" "${Ray_RunTasks:-}" >> "$CALL_FILE"
EOF
  chmod +x "$expected/bin/Ray.BiliBiliTool.Console"
  cp "$source_root/platforms/qinglong/DefaultTasks/"bili_task_{daily,base}.sh "$QL_DIR/data/scripts/"
  status=0
  (cd "$QL_DIR/data/scripts";unset dir_repo;BILI_MODE="$mode" bash ./bili_task_daily.sh) > "$case_dir/output" 2>&1 || status=$?
  if test "$status" = 0 && test ! -s "$NETWORK_FILE" && test "$(cat "$CALL_FILE")" = "$expected/src/Ray.BiliBiliTool.Console|Daily";then
   passed=$((passed+1))
  else failed=$((failed+1));printf 'FAIL daily/%s/%s\n' "$mode" "$layout" >&2;fi
 done
done
printf '{"cases":%s,"passed":%s,"failed":%s,"outside_cleanup_cases":%s,"network":"none","bilibili_requests":0,"notification_requests":0}\n' "$cases" "$passed" "$failed" "$unsafe"
test "${EXPECT_FAILURES:-0}" = 1 || test "$failed" = 0
