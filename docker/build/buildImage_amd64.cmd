@echo off

REM start to build
REM https://www.docker.com/blog/multi-arch-build-and-images-the-simple-way/
REM https://segmentfault.com/a/1190000021166703
echo Start to build docker image with amd64-arch
@echo on
docker buildx build --platform linux/amd64 -o type=docker -t zai7lou/bilibili_tool ../..
@echo off
pause
