#!/usr/bin/env bash
set -e
set -u
set -o pipefail

echo '  ____               ____    _   _____           _  '
echo ' |  _ \ __ _ _   _  | __ ) _| |_|_   _|__   ___ | | '
echo ' | |_) / _` | | | | |  _ \(_) (_) | |/ _ \ / _ \| | '
echo ' |  _ < (_| | |_| | | |_) | | | | | | (_) | (_) | | '
echo ' |_| \_\__,_|\__, | |____/|_|_|_| |_|\___/ \___/|_| '
echo '             |___/                                  '

# ------------vars-----------
repoDir=$(dirname $PWD)
consoleDir=$repoDir/src/Ray.BiliBiliTool.Console
publishDir=$consoleDir/bin/Publish
version=""
runTime=""
# --------------------------

read_params_from_init_cmd() {
    while [ $# -ne 0 ]; do
        name="$1"
        case "$name" in
        -r | --runtime | -[Rr]untime)
            shift
            runTime="$1"
            ;;
        *)
            say_err "Unknown argument \`$name\`"
            exit 1
            ;;
        esac
        shift
    done
}

read_var_from_user() {
    # runTime
    if [ -z "$runTime" ]; then
        read -p 'please input runTime("all" "win-x86" "win-x64" "win-arm64" "linux-x64" "linux-musl-x64" "linux-arm64" "linux-arm" "osx-x64")' runTime
    else
        echo "runTime: $runTime"
    fi
}

get_version() {
    version=$(grep -oP '(?<=<Version>).*?(?=<\/Version>)' $repoDir/common.props)
    echo -e "current version: $version \n\n"
}

publish_dotnet_dependent() {
    echo "---------start publishing 【dotnet dependent】 release---------"

    echo "clear output dir"
    local outputDir=$publishDir/dotnet-dependent
    mkdir -p $outputDir
    rm -rf $outputDir

    cd $consoleDir
    echo "dotnet publish..."
    dotnet publish --configuration Release \
        --self-contained false \
        -p:PublishSingleFile=true \
        -p:DebugType=None \
        -p:DebugSymbols=false \
        -o $outputDir

    echo "zip files..."
    cd $publishDir
    zip -q -r bilibili-tool-pro-v$version-dotnet-dependent.zip ./dotnet-dependent/*
    ls -l
    echo -e "---------publish successfully---------\n\n"
}

publish_self_contained() {
    local runtime=$1
    echo "---------start publishing 【$runtime】 release---------"

    echo "clear output dir"
    local outputDir=$publishDir/$runtime
    mkdir -p $outputDir
    rm -rf $outputDir

    cd $consoleDir
    echo "dotnet publish..."
    dotnet publish --configuration Release \
        --self-contained true \
        --runtime $runtime \
        -p:PublishTrimmed=true \
        -p:PublishSingleFile=true \
        -p:DebugType=None \
        -p:DebugSymbols=false \
        -o $outputDir

    echo "zip files..."
    cd $publishDir
    zip -q -r bilibili-tool-pro-v$version-$runtime.zip ./$runtime/*
    ls -l
    echo -e "---------publish successfully---------\n\n"
}

publish_tencentScf() {
    echo "---------start publishing 【tencent scf】 release---------"
    cd $publishDir
    cp -r $repoDir/tencentScf/bootstrap $repoDir/tencentScf/index.sh ./linux-x64/
    cd ./linux-x64
    chmod 755 index.sh bootstrap
    zip -r ../bilibili-tool-pro-v$version-tencent-scf.zip ./*
    cd .. && ls
    echo -e "---------publish successfully---------\n\n"
}

main() {
    read_params_from_init_cmd $*
    read_var_from_user

    get_version

    # dotnet dependent
    publish_dotnet_dependent

    # self contained
    # https://learn.microsoft.com/zh-cn/dotnet/core/rid-catalog
    array=("win-x86" "win-x64" "win-arm64" "linux-x64" "linux-musl-x64" "linux-arm64" "linux-arm" "osx-x64")
    if [ "$runTime" != "all" ]; then
        array=("$runTime")
    fi
    for i in "${array[@]}"; do
        publish_self_contained $i
    done

    if [ "$runTime" == "all" ]; then
        publish_tencentScf
    fi
}

main $*
