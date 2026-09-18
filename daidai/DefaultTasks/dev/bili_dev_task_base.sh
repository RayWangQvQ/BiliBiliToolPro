#!/usr/bin/env bash
# cron:0 0 1 1 *
# new Env("bili_dev_base")

# 先行版(dev)任务脚本（bili_dev_task_*.sh，由 daidai/copyshfile.sh 从 qinglong 复用过来）
# 会 source 本文件。这里不重复一份 base，直接复用上一级的 bili_task_base.sh——
# 其仓库根目录定位用的是“向上找 Ray.BiliBiliTool.sln”，stable 与 dev 都适用。
DEV_BASE_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" >/dev/null 2>&1 && pwd)"
. "$DEV_BASE_DIR/../bili_task_base.sh"
