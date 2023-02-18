#!/usr/bin/env bash
# new Env("bili天选时刻")
# cron 0 13 * * * bili_task_liveLottery.sh
. bili_task_base.sh

cd ./src/Ray.BiliBiliTool.Console

export Ray_RunTasks=LiveLottery && \
dotnet run
