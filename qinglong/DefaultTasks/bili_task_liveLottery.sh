#!/usr/bin/env bash
# new Env("bili天选时刻")
# cron 0 13 * * * bili_task_liveLottery.sh

cd "$(find /ql -type d -name "repo" -print)"
cd "$(find . -type d -name "raywangqvq_bilibilitoolpro" -print)"

dotnet run --project ./src/Ray.BiliBiliTool.Console --ENVIRONMENT=Production --runTasks=LiveLottery
