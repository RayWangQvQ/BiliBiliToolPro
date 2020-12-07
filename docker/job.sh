#!/bin/bash
configs=(`find /bilibili/config -maxdepth 1 -name "*.json"`)
if [ ${#configs[@]} -gt 0 ]; then 
	for ((idx=0; idx<${#configs[@]}; ++idx))
	do
		:
		echo Copying config file $idx
		cp ${configs[idx]} /bilibili/appsettings.json
		/bilibili/Ray.BiliBiliTool.Console -closeConsoleWhenEnd=1
		echo Execution finished
	done
else 
    echo No config files found in /bilibili/config
	exit 0
fi
