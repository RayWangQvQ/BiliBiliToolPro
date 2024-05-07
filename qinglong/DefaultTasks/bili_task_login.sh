#!/usr/bin/env bash
# new Env("bili扫码登录")
# cron 0 0 1 1 * bili_task_login.sh
. bili_task_base.sh

target_task_code="Login"
run_task "${target_task_code}"