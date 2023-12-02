dotnet.exe publish ../src/Ray.BiliBiliTool.Console/Ray.BiliBiliTool.Console.csproj --runtime win-x86 --no-self-contained -c Release -p:PublishSingleFile=true -o ./bin/Publish/win-x86
dotnet.exe publish ../src/Ray.BiliBiliTool.Console/Ray.BiliBiliTool.Console.csproj --runtime win-x64 --no-self-contained -c Release -p:PublishSingleFile=true -o ./bin/Publish/win-x64
dotnet.exe publish ../src/Ray.BiliBiliTool.Console/Ray.BiliBiliTool.Console.csproj --runtime win-arm64 --no-self-contained -c Release -p:PublishSingleFile=true -o ./bin/Publish/win-arm64
dotnet.exe publish ../src/Ray.BiliBiliTool.Console/Ray.BiliBiliTool.Console.csproj --runtime linux-x64 --no-self-contained -c Release -p:PublishSingleFile=true -o ./bin/Publish/linux-x64
dotnet.exe publish ../src/Ray.BiliBiliTool.Console/Ray.BiliBiliTool.Console.csproj --runtime linux-musl-x64 --no-self-contained -c Release -p:PublishSingleFile=true -o ./bin/Publish/linux-musl-x64
dotnet.exe publish ../src/Ray.BiliBiliTool.Console/Ray.BiliBiliTool.Console.csproj --runtime linux-arm64 --no-self-contained -c Release -p:PublishSingleFile=true -o ./bin/Publish/linux-arm64
dotnet.exe publish ../src/Ray.BiliBiliTool.Console/Ray.BiliBiliTool.Console.csproj --runtime linux-arm --no-self-contained -c Release -p:PublishSingleFile=true -o ./bin/Publish/linux-arm
dotnet.exe publish ../src/Ray.BiliBiliTool.Console/Ray.BiliBiliTool.Console.csproj --runtime osx-x64 --no-self-contained -c Release -p:PublishSingleFile=true -o ./bin/Publish/osx-x64
Remove-Item ./bin/Publish/win-x86/*.pdb
Remove-Item ./bin/Publish/win-x64/*.pdb
Remove-Item ./bin/Publish/win-arm64/*.pdb
Remove-Item ./bin/Publish/linux-x64/*.pdb
Remove-Item ./bin/Publish/linux-musl-x64/*.pdb
Remove-Item ./bin/Publish/linux-arm64/*.pdb
Remove-Item ./bin/Publish/linux-arm/*.pdb
Remove-Item ./bin/Publish/osx-x64/*.pdb
