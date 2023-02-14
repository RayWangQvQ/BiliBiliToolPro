#!/usr/bin/env bash
# new Env("bili_base")
# cron 0 0 1 1 * bili_base.sh

dir_shell=${QL_DIR-'/ql'}/shell
. $dir_shell/share.sh

## 安装dotnet（如果未安装过）
dotnetVersion=$(dotnet --version)
if [[ $dotnetVersion == 6.* ]]; then
    echo "已安装dotnet，当前版本：$dotnetVersion"
else
    echo "开始安装dotnet"
    {
        echo "尝试使用apk安装"
        apk add dotnet6-sdk
        dotnet --version
    } || {
        echo "尝试使用官方脚本安装"
        curl -sSL https://ghproxy.com/https://raw.githubusercontent.com/RayWangQvQ/BiliBiliToolPro/main/qinglong/ray-dotnet-install.sh | bash /dev/stdin
        dotnet --version
    } || {
        echo "尝试使用二进制包安装"
        curl -sSL https://ghproxy.com/https://raw.githubusercontent.com/RayWangQvQ/BiliBiliToolPro/main/qinglong/ray-dotnet-install.sh | bash /dev/stdin --no-official
        dotnet --version
    } || {
        echo "安装失败，没办法了，自己解决吧"
        exit 1
    }
fi

bili_repo="raywangqvq_bilibilitoolpro"

echo -e "\nrepo目录: $dir_repo"
bili_repo_dir="$(find $dir_repo -type d -iname $bili_repo | head -1)"
echo -e "bili仓库目录: $bili_repo_dir\n"

cd $bili_repo_dir
export Ray_PlateformType=QingLong
