#!/usr/bin/env bash
# new Env("bili每日任务[dev先行版]")
# cron 0 9 * * * bili_dev_task_daily.sh

cd "$(find /ql -type d -name "repo" -print)"
cd "$(find . -type d -name "raywangqvq_bilibilitoolpro_develop" -print)"

dotnet run --project ./src/Ray.BiliBiliTool.Console --ENVIRONMENT=Production --runTasks=Daily
