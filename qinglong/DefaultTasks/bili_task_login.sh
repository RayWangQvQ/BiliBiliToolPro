#!/usr/bin/env bash
# new Env("bili扫码登录")
# cron 0 0 1 1 * bili_task_login.sh
. bili_task_base.sh

cd ./src/Ray.BiliBiliTool.Console

if [ "$prefer_mode" == "dotnet" ]; then
    export Ray_RunTasks=Login && dotnet run
else
    export Ray_RunTasks=Login && ../../bin/Ray.BiliBiliTool.Console
fi