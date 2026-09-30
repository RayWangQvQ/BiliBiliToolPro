# Docker deployment

The current container runs the Web panel. Choose the installer, Docker Compose, or a direct `docker run` command.

## Prerequisites

Install Docker (and the Compose plugin if using Compose). On Debian/Ubuntu, install `curl` for the one-command installer:

```bash
apt-get update
apt-get install curl
```

## Option 1: installer script

The script attempts to install Docker if missing, downloads the Compose sample into `bili_tool_web/`, and starts the container (falling back to `docker run` if Compose is unavailable). Review a remote script before running it:

```bash
bash <(curl -sSL https://raw.githubusercontent.com/RayWangQvQ/BiliBiliToolPro/main/platforms/docker/install.sh)
```

## Option 2: Docker Compose

### Start

```bash
mkdir bili_tool_web && cd bili_tool_web
wget https://raw.githubusercontent.com/RayWangQvQ/BiliBiliToolPro/main/platforms/docker/sample/docker-compose.yml
docker compose up -d
docker logs -f bili_tool_web
```

The sample mounts `./Logs` and `./config` into the container and publishes port `22330` to container port `8080`. After the first run, the directory looks like:

```text
bili_tool_web/
├── Logs/
├── config/
│   └── BiliBiliTool.db
└── docker-compose.yml
```

### Other commands

```bash
docker compose up -d
docker compose stop
docker logs -f bili_tool_web
docker exec -it bili_tool_web /bin/bash
docker compose pull && docker compose up -d
```

## Option 3: direct Docker commands

### Start

```bash
mkdir bili_tool_web && cd bili_tool_web
docker pull ghcr.io/raywangqvq/bili_tool_web
docker run -d --name="bili_tool_web" \
    -p 22330:8080 \
    -e TZ=Asia/Shanghai \
    -v "$(pwd)/Logs:/app/Logs" \
    -v "$(pwd)/config:/app/config" \
    ghcr.io/raywangqvq/bili_tool_web
docker logs -f bili_tool_web
```

Add Bilibili accounts by scanning a QR code in the Web panel. Accounts are stored in `config/BiliBiliTool.db`; the Web panel no longer reads `config/cookies.json`. If an older installation has accounts only in that JSON file, add them again in the panel. The old file is not migrated or removed automatically.

### Container management

```bash
docker start bili_tool_web
docker stop bili_tool_web
docker restart bili_tool_web
docker rm bili_tool_web
docker exec -it bili_tool_web /bin/bash
```

### One-time update with Watchtower

```bash
docker run --rm \
    -v /var/run/docker.sock:/var/run/docker.sock \
    containrrr/watchtower \
    --run-once --cleanup \
    bili_tool_web
```

## Sign in and add an account

Open the Web panel on port `22330`. The initial username is `admin` and the initial password is `BiliTool@2233`. Change the password on the **Admin** page after your first sign-in. Scan the QR code to add a Bilibili account:

![Trigger account sign-in](../../docs/imgs/web-trigger-login.png)

![Scan the login QR code](../../docs/imgs/docker-login.png)

## Build your own image (optional)

Published images include [Docker Hub (`zai7lou/bili_tool_web`)](https://hub.docker.com/repository/docker/zai7lou/bili_tool_web) and [GitHub Container Registry (`bili_tool_web`)](https://github.com/RayWangQvQ/BiliBiliToolPro/pkgs/container/bili_tool_web). To build locally, run this from the repository root, where `Dockerfile` is located:

```bash
docker build -t TARGET_NAME .
```

Replace `TARGET_NAME` with your chosen image name and tag. The Dockerfile builds with `mcr.microsoft.com/dotnet/sdk:10.0` and runs with `mcr.microsoft.com/dotnet/aspnet:10.0`.

If GitHub downloads fail, check network access before choosing a proxy; third-party proxy availability varies.
