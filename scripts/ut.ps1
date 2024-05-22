Set-Location ..

# 安装 ReportGenerator 工具
Write-Output "Installing ReportGenerator tool..."
dotnet tool install -g dotnet-reportgenerator-globaltool

# 运行单元测试并生成覆盖率报告
Write-Output "Running unit tests and generating coverage report..."
dotnet test /p:CollectCoverage=true /p:CoverletOutputFormat=opencover /p:CoverletOutput=./TestResults/coverage.opencover.xml

# 生成html报告
$coverageFiles = Get-ChildItem -Path . -Recurse -Filter "coverage.cobertura.xml"
$coverageFiles | ForEach-Object { Write-Output $_.FullName }
$reportPaths = ($coverageFiles | ForEach-Object { $_.FullName }) -join ";"
reportgenerator "-reports:$reportPaths" "-targetdir:coveragereport" -reporttypes:Html

# 检查生成的覆盖率报告文件是否存在
Write-Output "Coverage report generated successfully."
Start-Process "coveragereport/index.htm"