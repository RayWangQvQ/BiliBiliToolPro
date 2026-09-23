::https://docs.microsoft.com/zh-cn/dotnet/core/tools/dotnet-publish

cd ../src/Ray.BiliBiliTool.Console

dotnet publish --configuration Release --runtime linux-x64 --self-contained true -p:PublishTrimmed=true -o ./bin/Publish/tencent-scf
copy /y ..\..\tencentScf\* .\bin\Publish\tencent-scf\

pause
