#!/usr/bin/env bash
# cron:0 14 * * *
# new Env("bili漫画任务")

. bili_task_base.sh

target_task_code="Manga"
run_bilitool_job "${target_task_code}"
