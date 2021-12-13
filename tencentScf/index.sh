echo "成功加载index.sh函数文件"

function main_handler () {
    echo "进入main_handler"
    EVENT_DATA=$1
    echo "$EVENT_DATA" 1>&2;
    runTasks=""
    if [[ $EVENT_DATA == *Message* ]]
    then
        eventMsg=$(echo $EVENT_DATA | grep -Po 'Message[" :]+\K[^"]+')
        echo "触发事件中的附加消息（任务编码）为：$eventMsg"
        runTasks="-runTasks=$eventMsg"
    else
	    echo "触发事件中未包含附加消息（任务编码）"
    fi
    echo "开始运行BiliBiliTool......"
    ./Ray.BiliBiliTool.Console $runTasks
    echo "函数结束"
}
