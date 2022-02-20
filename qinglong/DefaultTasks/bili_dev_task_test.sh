#!/usr/bin/env bash
# new Env("bili测试ck[dev先行版]")
# cron 0 8 * * * bili_dev_task_test.sh

dotnet run --project /ql/repo/raywangqvq_bilibilitoolpro_develop/src/Ray.BiliBiliTool.Console --ENVIRONMENT=Production --runTasks=Test
