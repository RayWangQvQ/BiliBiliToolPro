@echo off

REM start to build
echo Start to build image
@echo on
podman build -t docker.io/zai7lou/bilibili_tool_pro:0.2.2 -t docker.io/zai7lou/bilibili_tool_pro:latest ../..
@echo off
pause
