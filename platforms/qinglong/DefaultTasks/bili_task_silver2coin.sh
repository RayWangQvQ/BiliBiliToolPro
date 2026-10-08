#!/usr/bin/env bash
# cron:0 8 * * *
# new Env("bili银瓜子兑换硬币任务")

. bili_task_base.sh

target_task_code="Silver2Coin"
run_bilitool_job "${target_task_code}"
