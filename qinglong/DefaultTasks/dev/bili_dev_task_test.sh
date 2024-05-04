#!/usr/bin/env bash
# new Env("bili测试ck[dev先行版]")
# cron 0 8 * * * bili_dev_task_test.sh
. bili_dev_task_base.sh

cd ./src/Ray.BiliBiliTool.Console

if [ "$prefer_mode" == "dotnet" ]; then
    export Ray_RunTasks=Test && dotnet run
else
    export Ray_RunTasks=Test && ../../bin/Ray.BiliBiliTool.Console
fi