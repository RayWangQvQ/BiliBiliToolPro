#!/usr/bin/env bash
# new Env("bili_dev_task_base")
# cron 0 0 1 1 * bili_dev_task_base.sh

dir_shell=${QL_DIR-'/ql'}/shell
. $dir_shell/share.sh

## 安装dotnet（如果未安装过）
apk add dotnet6-sdk
dotnet --version

bili_repo="raywangqvq_bilibilitoolpro_develop"

echo -e "\nrepo目录: $dir_repo"
bili_repo_dir="$(find $dir_repo -type d -iname $bili_repo | head -1)"
echo -e "bili仓库目录: $bili_repo_dir\n"

cd $bili_repo_dir
export Ray_PlateformType=QingLong