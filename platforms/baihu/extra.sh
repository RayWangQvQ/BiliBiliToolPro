## 添加你需要重启自动执行的任意命令，比如 ql repo
## 安装node依赖使用 pnpm install -g xxx xxx
## 安装python依赖使用 pip3 install xxx

# 安装 dotnet 环境
if command -v mise >/dev/null 2>&1; then
    if mise install dotnet@10; then
        echo "已通过 mise 安装 dotnet@10"
    else
        echo "通过 mise 安装 dotnet@10 失败"
        exit 1
    fi
else
    echo "当前环境不支持 mise，无法安装 dotnet@10"
    exit 1
fi

# 其他代码...
