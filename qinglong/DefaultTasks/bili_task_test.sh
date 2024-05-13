#!/usr/bin/env bash
# cron:0 8 * * *
# new Env("bili测试ck")

. bili_task_base.sh

target_task_code="Test"
run_task "${target_task_code}"