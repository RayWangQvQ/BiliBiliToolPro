# https://docs.microsoft.com/zh-cn/dotnet/core/tools/dotnet-publish

cd ../src/Ray.BiliBiliTool.Console

dotnet publish --configuration Release --runtime linux-x64 --self-contained true -p:PublishTrimmed=true -o ./bin/Publish/tencent-scf
cp -r ../../tencentScf/bootstrap ../../tencentScf/index.sh ./bin/Publish/tencent-scf/

cd ./bin/Publish/tencent-scf/
zip -r ../tencent-scf.zip ./

pause