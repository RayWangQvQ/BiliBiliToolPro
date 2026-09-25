#!/usr/bin/env bash

# 获取项目根目录
# 优先使用环境变量 CURR_REPO_DIR，如果不存在则从当前脚本路径推算
if [ -z "${CURR_REPO_DIR:-}" ]; then
    CURRENT_FILE_DIR="$( cd "$( dirname "${BASH_SOURCE[0]}" )" && pwd )"
    REPO_ROOT="$(dirname "$CURRENT_FILE_DIR")"
else
    REPO_ROOT="$CURR_REPO_DIR"
fi

SRC_ROOT="$REPO_ROOT/qinglong/DefaultTasks"
DST_ROOT="$REPO_ROOT/baihu/DefaultTasks"



# 1. 处理根目录 (bili_task_*)
echo ">>> 正在从 $SRC_ROOT 同步任务脚本..."
for file in "$SRC_ROOT"/bili_task_*.sh; do
    # 确保文件存在（防止通配符未匹配）
    [ -e "$file" ] || continue
    
    filename=$(basename "$file")
    # 排除 base 文件
    if [[ "$filename" == "bili_task_base.sh" ]]; then
        continue
    fi
    
    cp -f "$file" "$DST_ROOT/"
    echo "已同步: $filename"
done

# 2. 处理 dev 目录 (bili_dev_task_*)
echo ">>> 正在从 $SRC_ROOT/dev 同步 dev 任务脚本..."
mkdir -p "$DST_ROOT/dev"
for file in "$SRC_ROOT"/dev/bili_dev_task_*.sh; do
    # 确保文件存在
    [ -e "$file" ] || continue
    
    filename=$(basename "$file")
    # 排除 base 文件
    if [[ "$filename" == "bili_dev_task_base.sh" ]]; then
        continue
    fi
    
    cp -f "$file" "$DST_ROOT/dev/"
    echo "已同步: dev/$filename"
done

echo ">>> 同步完成。"

# 3. 清理 qinglong 目录
echo ">>> 正在清理 qinglong 目录..."
rm -rf "$REPO_ROOT/qinglong"
echo ">>> 清理完成。"