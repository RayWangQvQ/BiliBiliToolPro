#!/usr/bin/env bash
# new Env("bili批量取关主播[dev先行版]")
# cron 0 12 1 * * bili_dev_task_unfollowBatched.sh

cd "$(find /ql -type d -name "repo" -print)"
cd "$(find . -type d -name "raywangqvq_bilibilitoolpro_develop" -print)"

dotnet run --project ./src/Ray.BiliBiliTool.Console --ENVIRONMENT=Production --runTasks=UnfollowBatched
