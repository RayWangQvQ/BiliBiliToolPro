#!/usr/bin/env bash
# new Env("bili天选时刻")
# cron 0 13 * * * bili_task_liveLottery.sh

dotnet run --project /ql/repo/raywangqvq_bilibilitoolpro/src/Ray.BiliBiliTool.Console -runTasks=LiveLottery
