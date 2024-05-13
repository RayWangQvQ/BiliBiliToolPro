#!/usr/bin/env bash
# cron:5 9 * * * bili_dev_task_daily.sh
#

. bili_dev_task_base.sh

target_task_code="Daily"
run_task "${target_task_code}"
