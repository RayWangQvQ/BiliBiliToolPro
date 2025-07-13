#!/bin/bash
set -e

echo "Starting BiliTool container..."

echo "Running maintenance scripts..."

# 3.3.0 need migrate db file location to /app/config
if [ -f "/app/BiliBiliTool.db" ]; then
    echo "[3.3.0]Migrate db file location to /app/config"
    mkdir -p /app/config
    mv /app/BiliBiliTool.db /app/config/BiliBiliTool.db
fi

echo "Starting application..."
exec dotnet Ray.BiliBiliTool.Web.dll "$@"