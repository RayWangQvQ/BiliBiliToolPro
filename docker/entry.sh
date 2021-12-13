#!/bin/bash
set -e

# https://stackoverflow.com/questions/27771781/how-can-i-access-docker-set-environment-variables-from-a-cron-job
echo "[step 1/4]导入环境变量"
printenv | grep -v "no_proxy" > /etc/environment
echo "=>完成"

echo "[step 2/4]配置cron定时任务"
myarray=(`find /app -maxdepth 1 -name "custom_crontab"`)
if [ ${#myarray[@]} -gt 0 ]; then 
	echo "=>检测到自定义了cron定时任务，使用自定义配置"
	cp /app/custom_crontab /etc/cron.d/bilicron
else
	echo "=>使用默认cron定时任务配置"
	cp /app/crontab /etc/cron.d/bilicron
fi
echo "=>完成"

echo "[step 3/4]启动定时任务，开启每日定时运行"
chmod 0644 /etc/cron.d/bilicron
crontab /etc/cron.d/bilicron
touch /var/log/cron.log
cron
echo "=>完成"

echo "[step 4/4]初始启动容器，尝试测试Cookie"
dotnet /app/Ray.BiliBiliTool.Console.dll -runTasks=Test
echo "=>完成"

echo "[step 全部已完成]"

tail -f /var/log/cron.log # 追踪cron日志
