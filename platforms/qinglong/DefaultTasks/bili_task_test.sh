#!/usr/bin/env bash
# cron:0 8 * * *
# new Env("bili测试ck")

. bili_task_base.sh

target_task_code="Test"
run_bilitool_job "${target_task_code}"
