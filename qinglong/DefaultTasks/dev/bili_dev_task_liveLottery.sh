#!/usr/bin/env bash
# new Env("bili天选时刻[dev先行版]")
# cron 0 13 * * * bili_dev_task_liveLottery.sh
. bili_dev_task_base.sh

target_task_code="LiveLottery"
run_task "${target_task_code}"