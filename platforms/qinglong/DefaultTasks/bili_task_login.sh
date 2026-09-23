#!/usr/bin/env bash
# cron:0 0 1 1 *
# new Env("bili扫码登录")

. bili_task_base.sh

target_task_code="Login"
run_task "${target_task_code}"