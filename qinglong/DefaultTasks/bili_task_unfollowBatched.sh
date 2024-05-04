#!/usr/bin/env bash
# new Env("bili批量取关主播")
# cron 0 12 1 * * bili_task_unfollowBatched.sh
. bili_task_base.sh

cd ./src/Ray.BiliBiliTool.Console

if [ "$prefer_mode" == "dotnet" ]; then
    export Ray_RunTasks=UnfollowBatched && dotnet run
else
    export Ray_RunTasks=UnfollowBatched && ../../bin/Ray.BiliBiliTool.Console
fi