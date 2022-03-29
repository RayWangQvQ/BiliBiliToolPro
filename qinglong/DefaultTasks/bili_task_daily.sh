#!/usr/bin/env bash
# new Env("bili每日任务")
# cron 0 9 * * * bili_task_daily.sh

cd "$(find /ql -type d -name "repo" -print)"
cd "$(find . -type d -name "raywangqvq_bilibilitoolpro" -print)"

dotnet run --project ./src/Ray.BiliBiliTool.Console --ENVIRONMENT=Production --runTasks=Daily
