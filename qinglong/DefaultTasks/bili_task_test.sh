#!/usr/bin/env bash
# new Env("bili测试ck")
# cron 0 8 * * * bili_task_test.sh

cd "$(find /ql -type d -name "repo" -print)"
cd "$(find . -type d -name "raywangqvq_bilibilitoolpro" -print)"

dotnet run --project ./src/Ray.BiliBiliTool.Console --ENVIRONMENT=Production --runTasks=Test
