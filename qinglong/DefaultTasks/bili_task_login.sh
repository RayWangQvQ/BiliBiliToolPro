#!/usr/bin/env bash
# new Env("bili扫码登录")
# cron 0 0 1 1 * bili_task_login.sh
. bili_task_base.sh

cd ./src/Ray.BiliBiliTool.Console

export Ray_RunTasks=Login && \
dotnet run
