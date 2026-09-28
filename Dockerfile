# syntax=docker/dockerfile:1
#See https://learn.microsoft.com/en-us/aspnet/core/host-and-deploy/docker/building-net-docker-images
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS base
WORKDIR /app
EXPOSE 8080

# 编译阶段强制跑在宿主机架构（BUILDPLATFORM）上，不再用 QEMU 模拟 arm64（ADR-0004）：
# RID-less publish 的产物是架构中立的 IL，全平台的 native 资源会一并进产物
# （如 runtimes/linux-arm64/native/libe_sqlite3.so），所以在 amd64 上编译出来的
# 与在 arm64 上编译出来的内容一致，而速度差约 10 倍（arm64 模拟 24.6 min，
# amd64 原生 2.9 min）。
FROM --platform=$BUILDPLATFORM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /code

# 版本一律由 CI 显式注入（构建时烘焙，ADR-0002）：
# VERSION 是完整版本号（alpha 的 x.y.z-alpha.N 或稳定版 x.y.z）；
# 本地手动 docker build 不传时，产物为 common.props 的 0.0.0-dev 兜底。
ARG VERSION=""

COPY ["Directory.Packages.props", "./"]
COPY ["src/Ray.BiliBiliTool.Web/Ray.BiliBiliTool.Web.csproj", "src/Ray.BiliBiliTool.Web/"]
COPY ["src/Ray.BiliBiliTool.Web.Client/Ray.BiliBiliTool.Web.Client.csproj", "src/Ray.BiliBiliTool.Web.Client/"]
COPY ["src/Ray.BiliBiliTool.Application/Ray.BiliBiliTool.Application.csproj", "src/Ray.BiliBiliTool.Application/"]
COPY ["src/Ray.BiliBiliTool.Application.Contracts/Ray.BiliBiliTool.Application.Contracts.csproj", "src/Ray.BiliBiliTool.Application.Contracts/"]
COPY ["src/Ray.BiliBiliTool.Domain/Ray.BiliBiliTool.Domain.csproj", "src/Ray.BiliBiliTool.Domain/"]
COPY ["src/Ray.BiliBiliTool.DomainService/Ray.BiliBiliTool.DomainService.csproj", "src/Ray.BiliBiliTool.DomainService/"]
COPY ["src/Ray.BiliBiliTool.Config/Ray.BiliBiliTool.Config.csproj", "src/Ray.BiliBiliTool.Config/"]
COPY ["src/Ray.BiliBiliTool.Agent/Ray.BiliBiliTool.Agent.csproj", "src/Ray.BiliBiliTool.Agent/"]
COPY ["src/Ray.BiliBiliTool.Infrastructure/Ray.BiliBiliTool.Infrastructure.csproj", "src/Ray.BiliBiliTool.Infrastructure/"]
COPY ["src/Ray.BiliBiliTool.Infrastructure.EF/Ray.BiliBiliTool.Infrastructure.EF.csproj", "src/Ray.BiliBiliTool.Infrastructure.EF/"]
COPY ["src/BlazingQuartz.Core/BlazingQuartz.Core.csproj", "src/BlazingQuartz.Core/"]
COPY ["src/BlazingQuartz.Jobs/BlazingQuartz.Jobs.csproj", "src/BlazingQuartz.Jobs/"]
COPY ["src/BlazingQuartz.Jobs.Abstractions/BlazingQuartz.Jobs.Abstractions.csproj", "src/BlazingQuartz.Jobs.Abstractions/"]

RUN dotnet restore "src/Ray.BiliBiliTool.Web/Ray.BiliBiliTool.Web.csproj"

COPY . .

# chmod 特意放在编译阶段（宿主机架构）做，好让 final 层一条 RUN 都不剩：
# 这样全程不执行任何目标架构指令，连 setup-qemu-action 都不必装。
RUN chmod +x platforms/docker/entrypoint.sh

WORKDIR "/code/src/Ray.BiliBiliTool.Web"

# 只 publish、不 build：原先 `dotnet build -o /app/build` 与 `dotnet publish -o /app/publish`
# 输出目录不同，会让增量判断失效、整套方案重编两遍（arm64 上白扔约 8 分钟）。
# --no-restore：assets 已由上面的 restore 层生成，publish 不必再还原一遍。
# UseAppHost=false：入口是 `dotnet Ray.BiliBiliTool.Web.dll`，用不到 apphost。
RUN version_arg="" \
    && if [ -n "$VERSION" ]; then version_arg="-p:Version=$VERSION"; fi \
    && dotnet publish "Ray.BiliBiliTool.Web.csproj" -c Release -o /app/publish --no-restore -p:UseAppHost=false $version_arg

FROM base AS final
ARG VERSION=""
LABEL org.opencontainers.image.version="${VERSION}"
WORKDIR /app
COPY --from=build /app/publish .
COPY --from=build /code/platforms/docker/entrypoint.sh /app/entrypoint.sh
ENTRYPOINT ["/app/entrypoint.sh"]
