#!/usr/bin/env bash
echo -e "\n-------set up dot net env-------"

## 安装dotnet
apk add bash icu-libs krb5-libs libgcc libintl libssl1.1 libstdc++ zlib
curl -sSL https://dot.net/v1/dotnet-install.sh | bash /dev/stdin --channel 6.0 --no-cdn --verbose

rm -f /usr/local/bin/dotnet
ln -s ~/.dotnet/dotnet /usr/local/bin

dotnet --info

echo -e "\n-------set up dot net env finish-------"