#!/usr/bin/env bash
###
# @Author: Ray zai7lou@outlook.com
# @Date: 2023-02-11 23:13:19
 # @LastEditors: Ray zai7lou@outlook.com
 # @LastEditTime: 2023-02-12 20:51:19
# @FilePath: \BiliBiliToolPro\docker\install.sh
# @Description:
###
set -e
set -u
set -o pipefail

echo '  ____               ____    _   _____           _  '
echo ' |  _ \ __ _ _   _  | __ ) _| |_|_   _|__   ___ | | '
echo ' | |_) / _` | | | | |  _ \(_) (_) | |/ _ \ / _ \| | '
echo ' |  _ < (_| | |_| | | |_) | | | | | | (_) | (_) | | '
echo ' |_| \_\__,_|\__, | |____/|_|_|_| |_|\___/ \___/|_| '
echo '             |___/                                  '

githubProxy="https://ghproxy.com/"

infFileName="ray-inf.sh"
[ -f $infFileName ] || {
    infUrl="${githubProxy}https://raw.githubusercontent.com/RayWangQvQ/ray-sh/main/$infFileName"
    (wget $infUrl) || (curl -sSL -f --create-dirs $infUrl) || {
        echo "未找到 wget 或 curl 命令，请自行安装"
        exit 1
    }
}
. ray-inf.sh

base_dir="/bili"
remote_compose_url="${githubProxy}https://raw.githubusercontent.com/RayWangQvQ/BiliBiliToolPro/main/docker/sample/docker-compose.yml"
remote_config_url="${githubProxy}https://raw.githubusercontent.com/RayWangQvQ/BiliBiliToolPro/main/src/Ray.BiliBiliTool.Console/appsettings.json"
remote_ckJson_url="${githubProxy}https://raw.githubusercontent.com/RayWangQvQ/BiliBiliToolPro/main/docker/sample/cookies.json"
docker_img_name="ghcr.io/raywangqvq/bilibili_tool_pro"

createBaseDir() {
    eval $invocation
    mkdir -p $base_dir
    cd $base_dir
}

installDocker() {
    eval $invocation
    if machine_has "docker"; then
        say_info "已安装docker"
        docker --version
        return 0
    else
        say_warning "未安装docker，尝试安装"
        download "https://get.docker.com" ./get-docker.sh
        chmod +x ./get-docker.sh
        get-docker.sh

        if machine_has "docker"; then
            say_info "已安装docker"
            docker --version
            return 0
        else
            say_err "docker 安装失败，请手动安装成功后再执行该脚本"
            exit 1
        fi
    fi
}

downloadResources() {
    eval $invocation
    say_info "开始下载资源"

    # docker compose
    [ -f "docker-compose.yml" ] || download $remote_compose_url ./docker-compose.yml

    # config
    [ -f "appsettings.json" ] || download $remote_config_url ./appsettings.json

    # ckJson
    [ -f "cookies.json" ] || download $remote_ckJson_url ./cookies.json

    ls -l
}

runContainer() {
    eval $invocation

    say_info "开始拉取镜像"
    docker pull $docker_img_name

    say_info "开始运行容器"
    {
        docker compose version && docker compose up -d
    } || {
        docker-compose version && docker-compose up -d
    } || {
        docker run -d --name="bili" \
            -v $base_dir/Logs:/app/Logs \
            -v $base_dir/appsettings.json:/app/appsettings.json \
            -v $base_dir/cookies.json:/app/cookies.json \
            $docker_img_name
    } || {
        say_err "创建容器失败，请检查"
        exit 1
    }
}

checkResult() {
    eval $invocation
    say_info "检测容器运行情况"

    local containerName="bili"
    docker ps --filter "name=$containerName"

    containerId=$(docker ps -q --filter "name=^$containerName$")
    if [ -n "$containerId" ]; then
        docker logs bili
        echo ""
        echo "==============================================="
        echo "Congratulations! 恭喜！"
        echo "创建并运行$containerName容器成功。"
        echo "第一次运行，请执行扫码登录："
        echo "docker exec -it $containerName dotnet /app/Ray.BiliBiliTool.Console.dll --runTasks=Login --Security__RandomSleepMaxMin=0"
        echo "Enjoy it~"
        echo "==============================================="
    else
        echo ""
        echo "请查看运行日志，确认容器是否正常运行，点击 Ctrl+c 退出日志追踪"
        echo ""
        docker logs -f $containerName
    fi
}

main() {
    installDocker
    createBaseDir
    downloadResources
    runContainer
    checkResult
}

main
