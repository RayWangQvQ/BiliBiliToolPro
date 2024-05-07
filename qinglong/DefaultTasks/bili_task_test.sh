#!/usr/bin/env bash
# new Env("bili测试ck")
# cron 0 8 * * * bili_task_test.sh
. bili_task_base.sh

target_task_code="Test"
run_task "${target_task_code}"