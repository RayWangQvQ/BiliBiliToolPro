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
# --------------------------

get_version() {
    version=$(grep -oP '(?<=<Version>).*?(?=<\/Version>)' $repoDir/common.props)
    echo -e "current version: $version \n"
}

publish_dotnet_dependent() {
    echo "---------start publishing dotnet dependent release---------"
    cd $consoleDir

    echo "dotnet publish..."
    dotnet publish --configuration Release --self-contained false -o $publishDir/dotnet-dependent
    echo "dotnet Ray.BiliBiliTool.Console.dll" >$publishDir/dotnet-dependent/start.bat

    echo "zip files..."
    cd $publishDir
    zip -q -r bilibili-tool-pro-v$version-dotnet-dependent.zip ./dotnet-dependent/*
    ls -l
    echo -e "---------publish successfully---------\n"
}

publish_win() {
    echo "---------start publishing win release---------"
    cd $consoleDir

    echo "dotnet publish..."
    dotnet publish --configuration Release --runtime win-x86 --self-contained true -p:PublishTrimmed=true -o $publishDir/win-x86-x64

    echo "zip files..."
    cd $publishDir
    zip -q -r bilibili-tool-pro-v$version-win-x86-x64.zip ./win-x86-x64/*
    echo -e "---------publish successfully---------\n"
}

publish_linux_arm() {
    echo "---------start publishing linux arm release---------"
    cd $consoleDir
    
    echo "dotnet publish..."
    dotnet publish --configuration Release --runtime linux-arm --self-contained true -p:PublishTrimmed=true -o $publishDir/linux-arm

    echo "zip files..."
    cd $publishDir
    zip -q -r bilibili-tool-pro-v$version-linux-arm.zip ./linux-arm/*
    echo -e "---------publish successfully---------\n"
}

publish_linux_x64() {
    echo "---------start publishing linux x64 release---------"
    cd $consoleDir

    echo "dotnet publish..."
    dotnet publish --configuration Release --runtime linux-x64 --self-contained true -p:PublishTrimmed=true -o $publishDir/linux-x64

    echo "zip files..."
    cd $publishDir
    zip -q -r bilibili-tool-pro-v$version-linux-x64.zip ./linux-x64/*
    echo -e "---------publish successfully---------\n"
}

publish_osx_x64() {
    echo "---------start publishing osx x64 release---------"
    cd $consoleDir

    echo "dotnet publish..."
    dotnet publish --configuration Release --runtime osx-x64 --self-contained true -p:PublishTrimmed=true -o $publishDir/osx-x64

    echo "zip files..."
    cd $publishDir
    zip -q -r bilibili-tool-pro-v$version-osx-x64.zip ./osx-x64/*
    echo -e "---------publish successfully---------\n"
}

publish_tencentScf() {
    echo "---------start publishing linux x64 release---------"
    cd $publishDir
    cp -r $repoDir/tencentScf/bootstrap $repoDir/tencentScf/index.sh ./linux-x64/
    cd ./linux-x64
    chmod 755 index.sh bootstrap
    zip -q -r ../bilibili-tool-pro-v$version-tencent-scf.zip ./*
    echo -e "---------publish successfully---------\n"
}

main() {
    get_version
    publish_dotnet_dependent
    publish_win
    publish_linux_arm
    publish_linux_x64
    publish_osx_x64
    publish_tencentScf
}

main
