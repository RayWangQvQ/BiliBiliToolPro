#!/usr/bin/env bash
# cron:0 12 1 * *
# new Env("bili批量取关主播[dev先行版]")

. bili_dev_task_base.sh

target_task_code="UnfollowBatched"
run_task "${target_task_code}"