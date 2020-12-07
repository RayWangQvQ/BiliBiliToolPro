# Dockerfile
## build
FROM mcr.microsoft.com/dotnet/sdk:5.0 AS build-env
WORKDIR ./

COPY . .

RUN dotnet restore
RUN dotnet build --configuration Release --no-restore -o out


## runtime
FROM mcr.microsoft.com/dotnet/aspnet:5.0
WORKDIR /bilibili

# install cron and nano
RUN apt update && apt-get install -y\
    cron \
    nano \
    && rm -rf /var/lib/apt/lists/*

# make mount point
RUN mkdir /bilibili/config

# copy files from build
COPY --from=build-env ./out .

# copy scripts
COPY ./docker/entry.sh ./docker/job.sh ./docker/template.json /bilibili/

# setup cron
COPY ./docker/crontab /etc/cron.d/bilicron
RUN chmod 0644 /etc/cron.d/bilicron \
	&& crontab /etc/cron.d/bilicron \
	&& touch /var/log/cron.log


VOLUME ["/bilibili/config"]
ENTRYPOINT ["/bin/bash", "-c", "/bilibili/entry.sh"]
