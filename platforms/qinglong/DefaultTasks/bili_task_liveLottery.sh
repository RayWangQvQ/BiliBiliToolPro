#!/usr/bin/env bash
# cron:0 13 * * *
# new Env("bili天选时刻")

. bili_task_base.sh

target_task_code="LiveLottery"
run_bilitool_job "${target_task_code}"
