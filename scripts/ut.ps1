Set-Location ..

# 安装 ReportGenerator 工具
Write-Output "Installing ReportGenerator tool..."
dotnet tool install -g dotnet-reportgenerator-globaltool

# 运行单元测试并生成覆盖率报告
Write-Output "Running unit tests and generating coverage report..."
dotnet test /p:CollectCoverage=true /p:CoverletOutputFormat=opencover

# 生成 HTML 格式的覆盖率报告
Write-Output "Generating HTML coverage report..."
reportgenerator "-reports:TestResults/*/coverage.opencover.xml" "-targetdir:coveragereport" -reporttypes:Html

# 打开生成的覆盖率报告
Write-Output "Opening coverage report..."
Start-Process "coveragereport/index.htm"