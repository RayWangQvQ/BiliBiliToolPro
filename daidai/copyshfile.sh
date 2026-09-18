#!/usr/bin/env bash

# 呆呆面板订阅「钩子脚本」：复用 qinglong 的各任务脚本，避免在 daidai 里重复维护。
# 配置方式：在订阅的「钩子脚本」里填  bash daidai/copyshfile.sh
# 运行时机：呆呆面板在“拉库之后、自动建任务之前”执行本钩子（CWD 即仓库目录）。
#
# 做的事：
#   1. 把 qinglong/DefaultTasks 下的 bili_task_*.sh（base 除外）拷到 daidai/DefaultTasks；
#   2. 把 qinglong/DefaultTasks/dev 下的 bili_dev_task_*.sh（base 除外）拷到 daidai/DefaultTasks/dev；
#   3. 删除 qinglong 目录，避免青龙版脚本（依赖 /ql 路径）被面板误登记成任务。
# 各任务脚本只是 `. bili_task_base.sh; run_task "Xxx"`，与面板无关，所以可直接复用；
# 真正面板相关的“环境安装/定位”只在 daidai 自己的 bili_task_base.sh 里实现。

set -e

# 仓库根目录：优先用呆呆面板注入的 SUB_DIR，否则从脚本自身位置推算
if [ -n "${SUB_DIR:-}" ]; then
    REPO_ROOT="$SUB_DIR"
else
    CURRENT_FILE_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
    REPO_ROOT="$(dirname "$CURRENT_FILE_DIR")"
fi

SRC_ROOT="$REPO_ROOT/qinglong/DefaultTasks"
DST_ROOT="$REPO_ROOT/daidai/DefaultTasks"

if [ ! -d "$SRC_ROOT" ]; then
    echo ">>> 未找到 $SRC_ROOT，跳过同步（可能已同步过）。"
    exit 0
fi

mkdir -p "$DST_ROOT"

echo ">>> 从 $SRC_ROOT 同步任务脚本到 $DST_ROOT ..."
for file in "$SRC_ROOT"/bili_task_*.sh; do
    [ -e "$file" ] || continue
    filename=$(basename "$file")
    # base 由 daidai 自己提供，不覆盖
    [ "$filename" = "bili_task_base.sh" ] && continue
    cp -f "$file" "$DST_ROOT/"
    echo "已同步: $filename"
done

echo ">>> 从 $SRC_ROOT/dev 同步先行版任务脚本 ..."
mkdir -p "$DST_ROOT/dev"
for file in "$SRC_ROOT"/dev/bili_dev_task_*.sh; do
    [ -e "$file" ] || continue
    filename=$(basename "$file")
    [ "$filename" = "bili_dev_task_base.sh" ] && continue
    cp -f "$file" "$DST_ROOT/dev/"
    echo "已同步: dev/$filename"
done

echo ">>> 清理 qinglong 目录，避免被重复登记成任务 ..."
rm -rf "$REPO_ROOT/qinglong"

echo ">>> 同步完成。"
