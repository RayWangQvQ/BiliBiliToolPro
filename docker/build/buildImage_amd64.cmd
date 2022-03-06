@echo off

REM start to build
REM https://www.docker.com/blog/multi-arch-build-and-images-the-simple-way/
REM https://segmentfault.com/a/1190000021166703
echo Start to build docker image with amd64-arch
@echo on
docker buildx build --tag zai7lou/bilibili_tool_pro:0.0.5 --output "type=image,push=false" --platform linux/amd64 ../..
@echo off
pause
