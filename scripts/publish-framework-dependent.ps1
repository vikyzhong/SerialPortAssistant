# 发布依赖本机 .NET 8 桌面运行时的 win-x64 版本（体积小，需用户自行安装运行时）
$ErrorActionPreference = "Stop"
$root = Split-Path -Parent (Split-Path -Parent $MyInvocation.MyCommand.Path)
Set-Location $root

$outRoot = Join-Path $root "publish\Win10"
$zipRoot = Join-Path $root "publish\releases"

Write-Host "Publishing SerialPortAssistant (framework-dependent)..."
dotnet publish SerialPortAssistant\SerialPortAssistant.csproj -c Release /p:PublishProfile=Win10-FrameworkDependent

Write-Host "Publishing SerialPortSimulator (framework-dependent)..."
dotnet publish SerialPortSimulator\SerialPortSimulator.csproj -c Release -r win-x64 --self-contained false `
  -o (Join-Path $outRoot "SerialPortSimulator")

New-Item -ItemType Directory -Force -Path $zipRoot | Out-Null

$assistantDir = Join-Path $outRoot "SerialPortAssistant"
$simulatorDir = Join-Path $outRoot "SerialPortSimulator"
$assistantZip = Join-Path $zipRoot "SerialPortAssistant-win-x64-fd.zip"
$simulatorZip = Join-Path $zipRoot "SerialPortSimulator-win-x64-fd.zip"

if (Test-Path $assistantZip) { Remove-Item $assistantZip -Force }
if (Test-Path $simulatorZip) { Remove-Item $simulatorZip -Force }

Compress-Archive -Path (Join-Path $assistantDir "*") -DestinationPath $assistantZip -CompressionLevel Optimal
Compress-Archive -Path (Join-Path $simulatorDir "*") -DestinationPath $simulatorZip -CompressionLevel Optimal

Write-Host ""
Write-Host "Done. Requires .NET 8 Desktop Runtime on target PC:"
Write-Host "  https://dotnet.microsoft.com/download/dotnet/8.0"
Write-Host ""
Write-Host "ZIP outputs:"
Write-Host "  $assistantZip"
Write-Host "  $simulatorZip"
