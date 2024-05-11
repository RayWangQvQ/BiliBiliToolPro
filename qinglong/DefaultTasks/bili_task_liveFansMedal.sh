#!/usr/bin/env bash
# 5 0 * * * bili_task_liveFansMedal.sh
# new Env("bili直播粉丝牌")

. bili_task_base.sh

target_task_code="LiveFansMedal"
run_task "${target_task_code}"