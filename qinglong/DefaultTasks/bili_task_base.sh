#!/usr/bin/env bash
# new Env("bili_base")
# cron 0 0 1 1 * bili_base.sh

dir_shell=${QL_DIR-'/ql'}/shell
. $dir_shell/share.sh
. /root/.bashrc

## 安装dotnet（如果未安装过）
dotnetVersion=$(dotnet --version)
if [[ $dotnetVersion == 6.* ]]; then
    echo "已安装dotnet，当前版本：$dotnetVersion"
else
    echo "which dotnet: $(which dotnet)"
    echo "开始安装dotnet"
    rayInstallShell="https://ghproxy.com/https://raw.githubusercontent.com/RayWangQvQ/BiliBiliToolPro/main/qinglong/ray-dotnet-install.sh"
    {
        echo "------尝试使用apk安装------"
        apk add dotnet6-sdk
        dotnet --version && echo "安装成功"
    } || {
        echo "------再尝试使用官方脚本安装------"
        curl -sSL $rayInstallShell | bash /dev/stdin
        . /root/.bashrc
        dotnet --version && echo "安装成功"
    } || {
        echo "------再尝试使用二进制包安装------"
        curl -sSL $rayInstallShell | bash /dev/stdin --no-official
        . /root/.bashrc
        dotnet --version && echo "安装成功"
    } || {
        echo "安装失败，没办法了，毁灭吧，自己解决吧：https://learn.microsoft.com/zh-cn/dotnet/core/install/linux-alpine"
        exit 1
    }
fi

bili_repo="raywangqvq_bilibilitoolpro"

echo -e "\nrepo目录: $dir_repo"
bili_repo_dir="$(find $dir_repo -type d -iname $bili_repo | head -1)"
echo -e "bili仓库目录: $bili_repo_dir\n"

cd $bili_repo_dir
export Ray_PlateformType=QingLong
export DOTNET_ENVIRONMENT=Production
