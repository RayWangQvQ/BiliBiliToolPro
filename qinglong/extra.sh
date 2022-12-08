## 添加你需要重启自动执行的任意命令，比如 ql repo
## 安装node依赖使用 pnpm install -g xxx xxx
## 安装python依赖使用 pip3 install xxx

# 安装 dotnet 环境
# curl -sSL https://ghproxy.com/https://raw.githubusercontent.com/RayWangQvQ/BiliBiliToolPro/main/qinglong/ray-dotnet-install.sh | bash /dev/stdin --no-official
curl -sSL https://ghproxy.com/https://raw.githubusercontent.com/RayWangQvQ/BiliBiliToolPro/main/qinglong/ray-dotnet-install.sh | bash /dev/stdin
# 安装需要用到的包
pip3 install requests qrcode pillow
# 其他代码...
