#!/usr/bin/env bash
# new Env("bili批量取关主播[dev先行版]")
# cron 0 12 1 * * bili_dev_task_unfollowBatched.sh
. bili_dev_task_base.sh

cd ./src/Ray.BiliBiliTool.Console

export Ray_RunTasks=UnfollowBatched && \
dotnet run --ENVIRONMENT=Production
