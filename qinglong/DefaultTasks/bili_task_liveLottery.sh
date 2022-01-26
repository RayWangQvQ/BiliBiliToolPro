#!/usr/bin/env bash
# bili天选时刻
# cron 0 13 * * * bili_task_test.sh

dotnet run --project /ql/repo/raywangqvq_bilibilitoolpro/src/Ray.BiliBiliTool.Console -runTasks=LiveLottery
