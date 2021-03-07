#!/bin/bash
set -e

echo "尝试首次运行"
/bin/bash /app/job.sh


echo "配置cron定时任务"
myarray=(`find /app -maxdepth 1 -name "custom_crontab"`)
if [ ${#myarray[@]} -gt 0 ]; then 
	echo "检测到自定义了cron定时任务，使用自定义配置"
	cp /app/custom_crontab /etc/cron.d/bilicron
else
	echo "使用默认cron定时任务配置"
	cp /app/crontab /etc/cron.d/bilicron
fi

chmod 0644 /etc/cron.d/bilicron
crontab /etc/cron.d/bilicron
touch /var/log/cron.log

# https://stackoverflow.com/questions/27771781/how-can-i-access-docker-set-environment-variables-from-a-cron-job
echo "导入环境变量"
printenv | grep -v "no_proxy" > /etc/environment

echo "启动定时任务，开启每日定时运行"
cron && tail -f /var/log/cron.log
