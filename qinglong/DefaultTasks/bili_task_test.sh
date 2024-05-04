#!/usr/bin/env bash
# new Env("bili测试ck")
# cron 0 8 * * * bili_task_test.sh
. bili_task_base.sh

cd ./src/Ray.BiliBiliTool.Console

if [ "$prefer_mode" == "dotnet" ]; then
    export Ray_RunTasks=Test && dotnet run
else
    export Ray_RunTasks=Test && ../../bin/Ray.BiliBiliTool.Console
fi