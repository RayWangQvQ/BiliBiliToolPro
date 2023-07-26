#See https://aka.ms/customizecontainer to learn how to customize your debug container and how Visual Studio uses this Dockerfile to build your images for faster debugging.

FROM mcr.microsoft.com/dotnet/aspnet:6.0 AS base
WORKDIR /app
EXPOSE 80
EXPOSE 443

FROM mcr.microsoft.com/dotnet/sdk:6.0 AS build
WORKDIR /src
COPY ["src/Ray.BiliTool.Blazor.Web/Ray.BiliTool.Blazor.Web.csproj", "src/Ray.BiliTool.Blazor.Web/"]
COPY ["src/Ray.BiliBiliTool.Application.Contracts/Ray.BiliBiliTool.Application.Contracts.csproj", "src/Ray.BiliBiliTool.Application.Contracts/"]
COPY ["src/Ray.BiliBiliTool.Application/Ray.BiliBiliTool.Application.csproj", "src/Ray.BiliBiliTool.Application/"]
COPY ["src/Ray.BiliBiliTool.Agent/Ray.BiliBiliTool.Agent.csproj", "src/Ray.BiliBiliTool.Agent/"]
COPY ["src/Ray.BiliBiliTool.Config/Ray.BiliBiliTool.Config.csproj", "src/Ray.BiliBiliTool.Config/"]
COPY ["src/Ray.BiliBiliTool.Infrastructure/Ray.BiliBiliTool.Infrastructure.csproj", "src/Ray.BiliBiliTool.Infrastructure/"]
COPY ["src/Ray.BiliBiliTool.DomainService/Ray.BiliBiliTool.DomainService.csproj", "src/Ray.BiliBiliTool.DomainService/"]
COPY ["src/Ray.BiliTool.Domain/Ray.BiliTool.Domain.csproj", "src/Ray.BiliTool.Domain/"]
COPY ["src/Ray.BiliTool.Repository/Ray.BiliTool.Repository.csproj", "src/Ray.BiliTool.Repository/"]
RUN dotnet restore "src/Ray.BiliTool.Blazor.Web/Ray.BiliTool.Blazor.Web.csproj"
COPY . .
WORKDIR "/src/src/Ray.BiliTool.Blazor.Web"
RUN dotnet build "Ray.BiliTool.Blazor.Web.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "Ray.BiliTool.Blazor.Web.csproj" -c Release -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "Ray.BiliTool.Blazor.Web.dll"]