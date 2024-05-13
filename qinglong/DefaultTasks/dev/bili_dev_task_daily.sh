#!/usr/bin/env bash
# cron:5 9 * * * qinglong/DefaultTasks/dev/bili_dev_task_daily.sh
# new Env('bili每日任务[dev先行版]');
#

. bili_dev_task_base.sh

target_task_code="Daily"
run_task "${target_task_code}"
