#!/usr/bin/env bash
# cron:5 0 * * *
# new Env("bili直播粉丝牌")

. bili_task_base.sh

target_task_code="LiveFansMedal"
run_bilitool_job "${target_task_code}"
