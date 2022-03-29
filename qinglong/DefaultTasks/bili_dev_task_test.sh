#!/usr/bin/env bash
# new Env("bili测试ck[dev先行版]")
# cron 0 8 * * * bili_dev_task_test.sh

cd "$(find /ql -type d -name "repo" -print)"
cd "$(find . -type d -name "raywangqvq_bilibilitoolpro_develop" -print)"

dotnet run --project ./src/Ray.BiliBiliTool.Console --ENVIRONMENT=Production --runTasks=Test
