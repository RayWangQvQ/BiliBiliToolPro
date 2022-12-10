#!/usr/bin/env bash
# new Env("bili每日任务[dev先行版]")
# cron 0 9 * * * bili_dev_task_daily.sh
. bili_dev_task_base.sh

cd ./src/Ray.BiliBiliTool.Console

export Ray_RunTasks=Daily && \
dotnet run --ENVIRONMENT=Production
