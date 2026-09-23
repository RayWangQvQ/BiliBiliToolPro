echo "start to build"
# https://www.docker.com/blog/multi-arch-build-and-images-the-simple-way/
# https://segmentfault.com/a/1190000021166703
# linux/arm/v6,linux/riscv64,linux/s390x,linux/ppc64le,linux/386,,linux/arm/v7 偶发异常，待进一步测试
echo "Start to build docker image with multi-arch"
# $image="zai7lou/bilibili_tool_pro"
# $version="0.0.5"
docker buildx build --tag "zai7lou/bilibili_tool_pro:0.0.5" --tag "zai7lou/bilibili_tool_pro:latest" --output "type=image,push=true" --platform linux/amd64,linux/arm64 ../..
