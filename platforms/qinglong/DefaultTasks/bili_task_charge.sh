#!/usr/bin/env bash
# cron:0 12 * * *
# new Env("bili免费B币券充电任务")

. bili_task_base.sh

target_task_code="Charge"
run_bilitool_job "${target_task_code}"
