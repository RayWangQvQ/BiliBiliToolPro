#See https://learn.microsoft.com/en-us/aspnet/core/host-and-deploy/docker/building-net-docker-images
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS base
WORKDIR /app
EXPOSE 8080

FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
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
WORKDIR "/code/src/Ray.BiliBiliTool.Web"
RUN version_arg="" \
    && if [ -n "$VERSION" ]; then version_arg="-p:Version=$VERSION"; fi \
    && dotnet build "Ray.BiliBiliTool.Web.csproj" -c Release -o /app/build $version_arg

FROM build AS publish
ARG VERSION=""
RUN version_arg="" \
    && if [ -n "$VERSION" ]; then version_arg="-p:Version=$VERSION"; fi \
    && dotnet publish "Ray.BiliBiliTool.Web.csproj" -c Release -o /app/publish $version_arg

FROM base AS final
ARG VERSION=""
LABEL org.opencontainers.image.version="${VERSION}"
WORKDIR /app
COPY --from=publish /app/publish .
COPY platforms/docker/entrypoint.sh /app/entrypoint.sh
RUN rm -rf /var/lib/apt/lists/* \
    && chmod +x /app/entrypoint.sh
ENTRYPOINT ["/app/entrypoint.sh"]
