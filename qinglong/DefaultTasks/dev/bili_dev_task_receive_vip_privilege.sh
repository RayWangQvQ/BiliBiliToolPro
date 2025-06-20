#!/usr/bin/env bash
# cron:0 9 * * *
# new Env("bili领取大会员福利任务[dev先行版]")

. bili_dev_task_base.sh

target_task_code="ReceiveVipPrivilege"
run_task "${target_task_code}"