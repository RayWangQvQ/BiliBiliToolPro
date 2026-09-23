#!/usr/bin/env bash
# cron:0 9 * * *
# new Env("bili每日任务")

. bili_task_base.sh

target_task_code="Daily"
run_task "${target_task_code}"