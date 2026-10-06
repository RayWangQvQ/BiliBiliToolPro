## 添加你需要重启自动执行的任意命令，比如 ql repo
## 安装node依赖使用 pnpm install -g xxx xxx
## 安装python依赖使用 pip3 install xxx

# 安装 dotnet 环境
# dotnet --version || (curl -sSL https://raw.githubusercontent.com/RayWangQvQ/BiliBiliToolPro/main/platforms/qinglong/ray-dotnet-install.sh | bash /dev/stdin --no-official) && (echo "已安装dotnet")
get_dotnet_major_version() {
    local dotnet_command="${1:-dotnet}"
    local dotnet_version
    # Respect SDK selection (including global.json) and reject failed commands.
    dotnet_version="$("$dotnet_command" --version 2>/dev/null)" || return 1
    # Some wrappers return SDK-list rows instead of one selected version.
    printf '%s\n' "$dotnet_version" | awk '
        /^[[:space:]]*[0-9]+\.[0-9]+\.[0-9]+(-[[:alnum:].-]+)?(\+[[:alnum:].-]+)?([[:space:]]+\[[^]]+\])?[[:space:]]*$/ {
            split($1, version, ".")
            if (version[1] + 0 > highest) highest = version[1] + 0
        }
        END {
            if (highest > 0) print highest
            else exit 1
        }
    '
}

dotnetMajor="$(get_dotnet_major_version dotnet || true)"
if ! [ "$dotnetMajor" -ge 10 ] 2>/dev/null; then
	if ! (set -o pipefail; curl -fsSL https://raw.githubusercontent.com/RayWangQvQ/BiliBiliToolPro/main/platforms/qinglong/ray-dotnet-install.sh | bash /dev/stdin); then
		echo "安装 .NET 10 SDK 失败"
		exit 1
	fi
	. /root/.bashrc
	dotnetMajor="$(get_dotnet_major_version dotnet || true)"
	if ! [ "$dotnetMajor" -ge 10 ] 2>/dev/null; then
		echo ".NET 10 SDK 安装后不可用"
		exit 1
	fi
	echo "已安装dotnet"
fi
# 其他代码...
