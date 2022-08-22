#!/bin/bash
set -e

# https://stackoverflow.com/questions/27771781/how-can-i-access-docker-set-environment-variables-from-a-cron-job
echo "[step 1/4]导入环境变量"
declare -p | grep -v "no_proxy" > /etc/cron.env
echo "=>完成"

echo "[step 2/4]配置cron定时任务"
echo "BASH_ENV=/etc/cron.env" > /etc/cron.d/bilicron
if ! [ -z $Ray_Crontab ]; then
	echo "=>检测到对应的环境变量，使用其值作为配置"
	echo $Ray_Crontab >> /etc/cron.d/bilicron
elif [ -e "/app/custom_crontab" ]; then 
	echo "=>检测到自定义了cron定时任务，使用自定义配置"
	cat /app/custom_crontab >> /etc/cron.d/bilicron
else
	echo "=>使用默认cron定时任务配置"
	cat /app/crontab >> /etc/cron.d/bilicron
fi
echo "=>完成"

echo "[step 3/4]启动定时任务，开启每日定时运行"
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
