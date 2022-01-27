#!/usr/bin/env bash
# new Env("bili批量取关主播")
# cron 0 12 1 * * bili_task_unfollowBatched.sh

dotnet run --project /ql/repo/raywangqvq_bilibilitoolpro/src/Ray.BiliBiliTool.Console -runTasks=UnfollowBatched
