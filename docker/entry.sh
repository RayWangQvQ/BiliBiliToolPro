#!/bin/bash
#https://stackoverflow.com/questions/3856747/check-whether-a-certain-file-type-extension-exists-in-directory

# if no configs are found, copy the template to configs directory
configs=(`find /bilibili/config -maxdepth 1 -name "*.json"`)
if [ ${#configs[@]} -eq 0 ]; then 
	cp /bilibili/template.json /bilibili/config
fi

echo Starting first run
/bin/bash /bilibili/job.sh

echo By default, Bilibilitool will run at 15:00 every day for each config file
echo To configure scheduling, run \'docker exec -it CONTAINER_NAME crontab -e\' and edit

cron && tail -f /var/log/cron.log
