#!/usr/bin/env bash
# 0 8 * * * bili_task_test.sh
# new Env("bili测试ck")

. bili_task_base.sh

target_task_code="Test"
run_task "${target_task_code}"