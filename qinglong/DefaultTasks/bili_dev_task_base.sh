#!/usr/bin/env bash
# new Env("bili_dev_task_base")
# cron 0 0 1 1 * bili_dev_task_base.sh

dir_shell=$QL_DIR/shell
. $dir_shell/share.sh

## 安装dotnet
apk add bash icu-libs krb5-libs libgcc libintl libssl1.1 libstdc++ zlib
curl -sSL https://dot.net/v1/dotnet-install.sh | bash /dev/stdin --channel 6.0 --no-cdn
rm -f /usr/local/bin/dotnet
ln -s ~/.dotnet/dotnet /usr/local/bin
dotnet --version

bili_repo="raywangqvq_bilibilitoolpro_develop"

echo -e "\nrepo目录: $dir_repo"
bili_repo_dir="$(find $dir_repo -type d -iname $bili_repo | head -1)"
echo -e "bili仓库目录: $bili_repo_dir\n"

cd $bili_repo_dir