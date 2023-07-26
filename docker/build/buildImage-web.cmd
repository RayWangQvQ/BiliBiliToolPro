@echo off

REM start to build
echo Start to build docker image
@echo on
docker build --tag zai7lou/bilitool:3.0.0 --tag zai7lou/bilitool:latest -f ../../src/Ray.BiliTool.Blazor.Web/Dockerfile ../..
@echo off
pause
