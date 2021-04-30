@echo off

REM start to build
REM https://www.docker.com/blog/multi-arch-build-and-images-the-simple-way/
REM https://segmentfault.com/a/1190000021166703
REM linux/arm/v6,linux/riscv64,linux/s390x,linux/ppc64le,linux/386,,linux/arm/v7 偶发异常，待进一步测试
echo Start to build docker image with multi-arch
@echo on
docker buildx build --platform linux/amd64,linux/arm64 --push -t zai7lou/bilibili_tool ../..
@echo off
pause
