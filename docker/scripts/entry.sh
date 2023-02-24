#!/bin/bash
set -e

. /app/scripts/entry_before.sh

CONSOLE_DLL="Ray.BiliBiliTool.Console.dll"
CRON_FILE="/etc/cron.d/bilicron"

# https://stackoverflow.com/questions/27771781/how-can-i-access-docker-set-environment-variables-from-a-cron-job
echo "[step 1/4]导入环境变量"
printenv | grep -v "no_proxy" >/etc/environment
declare -p | grep -v "no_proxy" >/etc/cron.env
echo -e "=>完成\n"

echo "[step 2/4]配置cron定时任务"
echo "SHELL=/bin/bash" >$CRON_FILE
echo "BASH_ENV=/etc/cron.env" >>$CRON_FILE
if [ -z "$Ray_DailyTaskConfig__Cron$Ray_LiveLotteryTaskConfig__Cron$Ray_UnfollowBatchedTaskConfig__Cron$Ray_VipBigPointConfig__Cron$Ray_LiveFansMedalTaskConfig__Cron" ]; then
	echo "=>使用默认的定时任务配置"
	cat /app/scripts/crontab >>$CRON_FILE
else
	echo "=>使用用户指定的定时任务配置"
	if ! [ -z "$Ray_DailyTaskConfig__Cron" ]; then
		echo "$Ray_DailyTaskConfig__Cron cd /app && dotnet $CONSOLE_DLL --runTasks=Daily" >>$CRON_FILE
	fi
	if ! [ -z "$Ray_LiveLotteryTaskConfig__Cron" ]; then
		echo "$Ray_LiveLotteryTaskConfig__Cron cd /app && dotnet $CONSOLE_DLL --runTasks=LiveLottery" >>$CRON_FILE
	fi
	if ! [ -z "$Ray_UnfollowBatchedTaskConfig__Cron" ]; then
		echo "$Ray_UnfollowBatchedTaskConfig__Cron cd /app && dotnet $CONSOLE_DLL --runTasks=UnfollowBatched" >>$CRON_FILE
	fi
	if ! [ -z "$Ray_VipBigPointConfig__Cron" ]; then
		echo "$Ray_VipBigPointConfig__Cron cd /app && dotnet $CONSOLE_DLL --runTasks=VipBigPoint" >>$CRON_FILE
	fi
	if ! [ -z "$Ray_LiveFansMedalTaskConfig__Cron" ]; then
		echo "$Ray_LiveFansMedalTaskConfig__Cron  cd /app && dotnet $CONSOLE_DLL --runTasks=LiveFansMedal" >>$CRON_FILE
	fi
fi

if ! [ -z "$Ray_Crontab" ]; then
	echo "=>检测到自定义定时任务"
	echo "$Ray_Crontab" >>$CRON_FILE
fi

cat $CRON_FILE
chmod 0644 $CRON_FILE
crontab $CRON_FILE # 指定定时列表文件

echo -e "=>完成\n"

echo "[step 3/4]启动定时任务，开启每日定时运行"
cron
echo -e "=>完成\n"

echo "[step 4/4]初始运行，尝试测试Cookie"
cd /app && dotnet Ray.BiliBiliTool.Console.dll --runTasks=Test
echo -e "=>完成\n"

echo -e "[step 全部已完成]\n"

. /app/scripts/entry_after.sh

touch /var/log/cron.log   #todo：debian似乎并没有记录cron的日志。。。
tail -f /var/log/cron.log # 追踪cron日志，避免当前脚本终止导致容器终止
