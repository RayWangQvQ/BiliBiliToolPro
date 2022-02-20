#!/usr/bin/env bash
# new Env("bili批量取关主播[dev先行版]")
# cron 0 12 1 * * bili_task_unfollowBatched.sh

dotnet run --project /ql/repo/raywangqvq_bilibilitoolpro_develop/src/Ray.BiliBiliTool.Console --ENVIRONMENT=Production --runTasks=UnfollowBatched
