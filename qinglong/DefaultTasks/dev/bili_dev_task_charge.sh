#!/usr/bin/env bash
# cron:0 12 * * *
# new Env("bili免费B币券充电任务[dev先行版]")

. bili_dev_task_base.sh

target_task_code="Charge"
run_task "${target_task_code}"
