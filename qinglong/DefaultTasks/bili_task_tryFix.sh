#!/usr/bin/env bash
# new Env("bili尝试修复异常")
# cron 0 0 1 1 * bili_task_tryFix.sh

dir_shell=$QL_DIR/shell
. $dir_shell/share.sh

bili_repo="raywangqvq_bilibilitoolpro_develop"

echo -e "\nrepo目录: $dir_repo"
bili_repo_dir="$(find $dir_repo -type d -iname $bili_repo | head -1)"
echo -e "bili仓库目录: $bili_repo_dir\n"

echo -e "清理缓存...\n"
cd $bili_repo_dir
find . -type d -name "bin" -exec rm -rf {} +
find . -type d -name "obj" -exec rm -rf {} +