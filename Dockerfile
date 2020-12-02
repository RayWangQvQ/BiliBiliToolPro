# Dockerfile
# build
FROM mcr.microsoft.com/dotnet/sdk:5.0 AS build-env
WORKDIR ./

COPY . .

RUN dotnet restore
RUN dotnet build --configuration Release --no-restore -o out


## runtime
FROM mcr.microsoft.com/dotnet/aspnet:5.0
WORKDIR /bilibili

# install cron and nano
RUN apt update
RUN apt install -qq -y cron nano

# copy files from build
COPY --from=build-env ./out .

# make mount point
RUN mkdir /bilibili/config

# copy scripts
COPY ./docker/entry.sh /bilibili
COPY ./docker/job.sh /bilibili
COPY ./docker/template.json /bilibili

# copy template config
COPY ./docker/template.json /bilibili/config

# setup cron
COPY ./docker/crontab /etc/cron.d/bilicron
RUN chmod 0644 /etc/cron.d/bilicron
RUN crontab /etc/cron.d/bilicron
RUN touch /var/log/cron.log


VOLUME ["/bilibili/config"]
ENTRYPOINT ["/bin/bash", "-c", "/bilibili/entry.sh"]
