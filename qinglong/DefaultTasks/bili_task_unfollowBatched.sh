#!/usr/bin/env bash
# new Env("bili批量取关主播")
# cron 0 12 1 * * bili_task_unfollowBatched.sh
. bili_task_base.sh

cd ./src/Ray.BiliBiliTool.Console

export Ray_RunTasks=UnfollowBatched && \
dotnet run
