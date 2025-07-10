#!/usr/bin/env bash
# cron:0 8 * * *
# new Env("bili银瓜子兑换硬币任务[dev先行版]")

. bili_dev_task_base.sh

target_task_code="Silver2Coin"
run_task "${target_task_code}"
