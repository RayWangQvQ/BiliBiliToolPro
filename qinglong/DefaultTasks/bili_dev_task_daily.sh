#!/usr/bin/env bash
# new Env("bili每日任务[dev先行版]")
# cron 0 9 * * * bili_task_daily.sh

dotnet run --project /ql/repo/raywangqvq_bilibilitoolpro_develop/src/Ray.BiliBiliTool.Console --ENVIRONMENT=Production --runTasks=Daily
