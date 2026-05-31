# install.ps1 - 一键安装脚本
$pluginName = "HolidayCountdown"
$sourceDir = ".\bin\Release\net8.0-windows"
$targetDir = "$env:LOCALAPPDATA\ClassIsland\Plugins\$pluginName"

if (-not (Test-Path "$env:LOCALAPPDATA\ClassIsland")) {
    $targetDir = ".\ClassIsland\Plugins\$pluginName"
}

Write-Host "安装目录: $targetDir"
New-Item -ItemType Directory -Force -Path $targetDir | Out-Null

Copy-Item "$sourceDir\$pluginName.dll" $targetDir -Force
Copy-Item "$sourceDir\$pluginName.deps.json" $targetDir -Force
Copy-Item ".\manifest.yml" $targetDir -Force
Copy-Item ".\icon.png" $targetDir -Force

$depsJson = Join-Path $targetDir "$pluginName.deps.json"
if (Test-Path $depsJson) {
    Write-Host "安装完成！请重启 ClassIsland。" -ForegroundColor Green
} else {
    Write-Host "错误：deps.json 未找到！" -ForegroundColor Red
}

Read-Host "按回车键退出"
