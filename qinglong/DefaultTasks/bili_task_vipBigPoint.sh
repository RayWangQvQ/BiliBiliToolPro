#!/usr/bin/env bash
# 7 1 * * * bili_task_vipBigPoint.sh
# new Env("bili大会员大积分")

. bili_task_base.sh

target_task_code="VipBigPoint"
run_task "${target_task_code}"