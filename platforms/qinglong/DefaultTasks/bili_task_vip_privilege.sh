#!/usr/bin/env bash
# cron:0 1 * * *
# new Env("bili领取大会员福利任务")

. bili_task_base.sh

target_task_code="VipPrivilege"
run_task "${target_task_code}"
