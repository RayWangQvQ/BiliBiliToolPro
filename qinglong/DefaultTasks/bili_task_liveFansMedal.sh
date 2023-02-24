#!/usr/bin/env bash
# new Env("bili直播粉丝牌")
# cron 5 0 * * * bili_task_liveFansMedal.sh
. bili_task_base.sh

cd ./src/Ray.BiliBiliTool.Console

export Ray_RunTasks=LiveFansMedal && \
dotnet run
