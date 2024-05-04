#!/usr/bin/env bash
# new Env("bili天选时刻")
# cron 0 13 * * * bili_task_liveLottery.sh
. bili_task_base.sh

cd ./src/Ray.BiliBiliTool.Console

if [ "$prefer_mode" == "dotnet" ]; then
    export Ray_RunTasks=LiveLottery && dotnet run
else
    export Ray_RunTasks=LiveLottery && ../../bin/Ray.BiliBiliTool.Console
fi