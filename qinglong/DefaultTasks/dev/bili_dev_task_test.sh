#!/usr/bin/env bash
# new Env("bili测试ck[dev先行版]")
# cron 0 8 * * * bili_dev_task_test.sh
. bili_dev_task_base.sh

target_task_code="Test"
run_task "${target_task_code}"