#!/usr/bin/env bash
# new Env("bili批量取关主播")
# cron 0 12 1 * * bili_task_unfollowBatched.sh

cd "$(find /ql -type d -name "repo" -print)"
cd "$(find . -type d -name "raywangqvq_bilibilitoolpro" -print)"

dotnet run --project ./src/Ray.BiliBiliTool.Console --ENVIRONMENT=Production --runTasks=UnfollowBatched
