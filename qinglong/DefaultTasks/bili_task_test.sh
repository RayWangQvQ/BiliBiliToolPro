#!/usr/bin/env bash

# cron 0 0 * * * bili_task_test.sh

dotnet run --project /ql/repo/raywangqvq_bilibilitoolpro/src/Ray.BiliBiliTool.Console --DOTNET_ENVIRONMENT Production -runTasks=Test
