#!/usr/bin/env bash
# new Env("bili直播粉丝牌")
# cron 5 0 * * * bili_task_liveFansMedal.sh
. bili_task_base.sh

target_task_code="LiveFansMedal"
run_task "${target_task_code}"