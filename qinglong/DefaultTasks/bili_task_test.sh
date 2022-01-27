#!/usr/bin/env bash
# new Env("bili测试ck")
# cron 0 8 * * * bili_task_test.sh

dotnet run --project /ql/repo/raywangqvq_bilibilitoolpro/src/Ray.BiliBiliTool.Console -runTasks=Test
