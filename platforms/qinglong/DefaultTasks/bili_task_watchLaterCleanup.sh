#!/usr/bin/env bash
# cron:30 3 * * *
# new Env("bili稍后再看清理")

. bili_task_base.sh

target_task_code="WatchLaterCleanup"
run_task "${target_task_code}"
