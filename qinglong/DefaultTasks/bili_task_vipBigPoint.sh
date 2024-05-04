#!/usr/bin/env bash
# new Env("bili大会员大积分")
# cron 7 1 * * * bili_task_vipBigPoint.sh
. bili_task_base.sh

cd ./src/Ray.BiliBiliTool.Console

if [ "$prefer_mode" == "dotnet" ]; then
    export Ray_RunTasks=VipBigPoint && dotnet run
else
    export Ray_RunTasks=VipBigPoint && ../../bin/Ray.BiliBiliTool.Console
fi