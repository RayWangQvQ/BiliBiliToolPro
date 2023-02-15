#!/usr/bin/env bash
# new Env("bili尝试修复异常")
# cron 0 0 1 1 * bili_task_tryFix.sh

dir_shell=$QL_DIR/shell
. $dir_shell/share.sh
. /root/.bashrc

bili_repo="raywangqvq_bilibilitoolpro"

echo -e "\nrepo目录: $dir_repo"
bili_repo_dir="$(find $dir_repo -type d -iname $bili_repo | head -1)"
echo -e "bili仓库目录: $bili_repo_dir\n"

echo -e "清理缓存...\n"
cd $bili_repo_dir
find . -type d -name "bin" -exec rm -rf {} +
find . -type d -name "obj" -exec rm -rf {} +
echo "清理完成"

echo "检测dotnet"
dotnetVersion=$(dotnet --version)
if [[ $dotnetVersion == 6.* ]]; then
    echo "已安装dotnet，当前版本：$dotnetVersion"
else
    echo "which dotnet: $(which dotnet)"
    echo "Paht: $PATH"
    rm -f /usr/local/bin/dotnet
fi