#!/bin/bash
set -e

# https://stackoverflow.com/questions/27771781/how-can-i-access-docker-set-environment-variables-from-a-cron-job
echo "[step 1/4]导入环境变量"
printenv | grep -v "no_proxy" > /etc/environment
declare -p | grep -v "no_proxy" > /etc/cron.env
echo "=>完成"

echo "[step 2/4]配置cron定时任务"
echo "BASH_ENV=/etc/cron.env" > /etc/cron.d/bilicron
if [ -z "$Ray_DailyTaskConfig__Cron$Ray_LiveLotteryTaskConfig__Cron$Ray_UnfollowBatchedTaskConfig__Cron$Ray_VipBigPointConfig__Cron" ]; then
	echo "=>使用默认的定时任务配置"
	cat /app/crontab >> /etc/cron.d/bilicron
else
	echo "=>使用用户指定的定时任务配置"
	if ! [ -z "$Ray_DailyTaskConfig__Cron" ]; then
		echo "$Ray_DailyTaskConfig__Cron dotnet /app/Ray.BiliBiliTool.Console.dll --runTasks=Daily >> /var/log/cron.log" >> /etc/cron.d/bilicron
	fi
	if ! [ -z "$Ray_LiveLotteryTaskConfig__Cron" ]; then
		echo "$Ray_LiveLotteryTaskConfig__Cron dotnet /app/Ray.BiliBiliTool.Console.dll --runTasks=LiveLottery >> /var/log/cron.log" >> /etc/cron.d/bilicron
	fi
	if ! [ -z "$Ray_UnfollowBatchedTaskConfig__Cron" ]; then
		echo "$Ray_UnfollowBatchedTaskConfig__Cron dotnet /app/Ray.BiliBiliTool.Console.dll --runTasks=UnfollowBatched >> /var/log/cron.log" >> /etc/cron.d/bilicron
	fi
	if ! [ -z "$Ray_VipBigPointConfig__Cron" ]; then
		echo "$Ray_VipBigPointConfig__Cron dotnet /app/Ray.BiliBiliTool.Console.dll --runTasks=VipBigPoint >> /var/log/cron.log" >> /etc/cron.d/bilicron
	fi
fi
if [ -z "$Ray_Crontab" ]; then
	echo "=>检测到额外定时任务，追加到cron文件末尾"
	echo "$Ray_Crontab" >> /etc/cron.d/bilicron
fi
echo "=>完成"

echo "[step 3/4]启动定时任务，开启每日定时运行"
cat /etc/cron.d/bilicron
chmod 0644 /etc/cron.d/bilicron
crontab /etc/cron.d/bilicron
touch /var/log/cron.log
cron
echo "=>完成"

echo "[step 4/4]初始启动容器，尝试测试Cookie"
dotnet /app/Ray.BiliBiliTool.Console.dll --runTasks=Test
echo "=>完成"

echo "[step 全部已完成]"

tail -f /var/log/cron.log # 追踪cron日志
