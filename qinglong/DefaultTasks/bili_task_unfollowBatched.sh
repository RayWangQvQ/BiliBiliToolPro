#!/usr/bin/env bash
# bili批量取关主播
# cron 0 12 1 * * bili_task_test.sh

dotnet run --project /ql/repo/raywangqvq_bilibilitoolpro/src/Ray.BiliBiliTool.Console -runTasks=UnfollowBatched
