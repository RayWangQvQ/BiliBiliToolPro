## build
FROM mcr.microsoft.com/dotnet/sdk:5.0 AS build-env
WORKDIR /

COPY . .

RUN dotnet restore\
    && dotnet build --configuration Release --no-restore -o out



## runtime
FROM mcr.microsoft.com/dotnet/aspnet:5.0
WORKDIR /app
ENV TIME_ZONE=Asia/Shanghai

# copy files from build
COPY --from=build-env ./out .

# copy scripts
COPY ./docker/entry.sh ./docker/crontab /app/

RUN ln -fs /usr/share/zoneinfo/$TIME_ZONE /etc/localtime\
    && echo $TIME_ZONE > /etc/timezone\
    && apt-get update \
    && apt-get install -y cron tzdata\
    && apt-get clean



ENTRYPOINT ["/bin/bash", "-c", "/app/entry.sh"]
