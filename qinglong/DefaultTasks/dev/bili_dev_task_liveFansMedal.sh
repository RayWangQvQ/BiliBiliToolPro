#!/usr/bin/env bash
# new Env("bili直播粉丝牌[dev先行版]")
# cron 5 0 * * * bili_dev_task_liveFansMedal.sh
. bili_dev_task_base.sh

target_task_code="LiveFansMedal"
run_task "${target_task_code}"