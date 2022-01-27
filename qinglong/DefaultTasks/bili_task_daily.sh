#!/usr/bin/env bash
# new Env("bili每日任务")
# cron 0 9 * * * bili_task_daily.sh

dotnet run --project /ql/repo/raywangqvq_bilibilitoolpro/src/Ray.BiliBiliTool.Console Production -runTasks=Daily
