#!/usr/bin/env bash
echo -e "\n-------set up dot net env-------"

required_dotnet_major=10

get_dotnet_major_version() {
    local dotnet_command="${1:-dotnet}"
    local dotnet_version
    dotnet_version="$("$dotnet_command" --version 2>/dev/null || true)"
    echo "${dotnet_version%%.*}"
}

has_required_dotnet_version() {
    local dotnet_major
    dotnet_major="$(get_dotnet_major_version "$1")"
    [[ "$dotnet_major" =~ ^[0-9]+$ && "$dotnet_major" -ge "$required_dotnet_major" ]]
}

configure_script_installed_dotnet_path() {
    local exportFile="/root/.bashrc"
    touch "$exportFile"
    echo '' >> "$exportFile"
    echo 'export DOTNET_ROOT=$HOME/.dotnet' >> "$exportFile"
    echo 'export PATH=$DOTNET_ROOT:$DOTNET_ROOT/tools:$PATH' >> "$exportFile"

    export DOTNET_ROOT="$HOME/.dotnet"
    export PATH="$DOTNET_ROOT:$DOTNET_ROOT/tools:$PATH"
}

configure_alpine_dotnet_path() {
    local exportFile="/root/.bashrc"
    touch "$exportFile"
    echo '' >> "$exportFile"
    echo 'unset DOTNET_ROOT' >> "$exportFile"
    echo 'export PATH=/usr/bin:$PATH' >> "$exportFile"

    unset DOTNET_ROOT
    export PATH="/usr/bin:$PATH"
    hash -r
}

install_by_official_script() {
    echo "install by official script..."
    curl -fsSL https://dot.net/v1/dotnet-install.sh | bash /dev/stdin --channel 10.0 --verbose
    configure_script_installed_dotnet_path
}

install_for_alpine() {
    if [[ "${VERSION_ID:-}" != 3.23* ]]; then
        echo "Alpine ${VERSION_ID:-unknown} does not provide .NET 10 packages for Qinglong."
        echo "Please upgrade to Alpine 3.23 or set BILI_MODE=bilitool."
        return 1
    fi

    echo "install .NET 10 SDK from Alpine package manager..."
    apk add --no-cache dotnet10-sdk
    configure_alpine_dotnet_path
}

install_dotnet() {
    if [[ -r /etc/os-release ]]; then
        . /etc/os-release
    fi

    if [[ "${ID:-}" == "alpine" ]]; then
        install_for_alpine
    else
        install_by_official_script
    fi
}

remove_legacy_dotnet_entry() {
    local legacy_dotnet="/usr/local/bin/dotnet"
    if [[ -e "$legacy_dotnet" || -L "$legacy_dotnet" ]] && ! has_required_dotnet_version "$legacy_dotnet"; then
        rm -f "$legacy_dotnet"
        hash -r
    fi
}

if has_required_dotnet_version dotnet; then
    echo ".NET $(dotnet --version) is already installed."
else
    install_dotnet
    if ! has_required_dotnet_version dotnet; then
        echo "Failed to install .NET $required_dotnet_major SDK."
        exit 1
    fi
fi

remove_legacy_dotnet_entry

dotnet --info

echo -e "\n-------set up dot net env finish-------"