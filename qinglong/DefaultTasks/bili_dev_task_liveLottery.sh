#!/usr/bin/env bash
# new Env("bili天选时刻[dev先行版]")
# cron 0 13 * * * bili_dev_task_liveLottery.sh

dotnet run --project /ql/repo/raywangqvq_bilibilitoolpro_develop/src/Ray.BiliBiliTool.Console --ENVIRONMENT=Production --runTasks=LiveLottery
