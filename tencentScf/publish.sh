# https://docs.microsoft.com/zh-cn/dotnet/core/tools/dotnet-publish

cd ../src/Ray.BiliBiliTool.Console

dotnet publish --configuration Release --runtime linux-x64 --self-contained true -p:PublishTrimmed=true -o ../../tencentScf/bin/publish

cd ../../tencentScf
cp -r ./bootstrap ./index.sh ./bin/publish/

cd ./bin/publish
chmod 755 index.sh bootstrap
zip -r -q ../tencent-scf.zip ./*