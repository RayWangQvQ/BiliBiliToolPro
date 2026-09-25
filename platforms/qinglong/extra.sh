## 添加你需要重启自动执行的任意命令，比如 ql repo
## 安装node依赖使用 pnpm install -g xxx xxx
## 安装python依赖使用 pip3 install xxx

# 安装 dotnet 环境
# dotnet --version || (curl -sSL https://raw.githubusercontent.com/RayWangQvQ/BiliBiliToolPro/main/platforms/qinglong/ray-dotnet-install.sh | bash /dev/stdin --no-official) && (echo "已安装dotnet")
dotnetVersion="$(dotnet --version 2>/dev/null || true)"
dotnetMajor="${dotnetVersion%%.*}"
if ! [ "$dotnetMajor" -ge 10 ] 2>/dev/null; then
	if ! (set -o pipefail; curl -fsSL https://raw.githubusercontent.com/RayWangQvQ/BiliBiliToolPro/main/platforms/qinglong/ray-dotnet-install.sh | bash /dev/stdin); then
		echo "安装 .NET 10 SDK 失败"
		exit 1
	fi
	. /root/.bashrc
	dotnetVersion="$(dotnet --version 2>/dev/null || true)"
	dotnetMajor="${dotnetVersion%%.*}"
	if ! [ "$dotnetMajor" -ge 10 ] 2>/dev/null; then
		echo ".NET 10 SDK 安装后不可用"
		exit 1
	fi
	echo "已安装dotnet"
fi
# 其他代码...
