@echo off

REM start to build
echo Start to build docker image
@echo on
docker build -t zai7lou/bilibili_tool ../..
@echo off
pause
