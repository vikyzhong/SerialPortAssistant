# 将 publish\releases\ 下的框架依赖 ZIP 上传到 GitHub Releases（需已安装并登录 gh）
# 用法：.\scripts\upload-github-release.ps1 -Tag v0.01
param(
    [string]$Tag = "v0.01",
    [string]$Title = "V0.01 — 框架依赖版 (win-x64)"
)

$ErrorActionPreference = "Stop"
$gh = "C:\Program Files\GitHub CLI\gh.exe"
if (-not (Get-Command $gh -ErrorAction SilentlyContinue)) {
    $gh = "gh"
}

$root = Split-Path -Parent (Split-Path -Parent $MyInvocation.MyCommand.Path)
$zipDir = Join-Path $root "publish\releases"
$assistant = Join-Path $zipDir "SerialPortAssistant-win-x64-fd.zip"
$simulator = Join-Path $zipDir "SerialPortSimulator-win-x64-fd.zip"

if (-not (Test-Path $assistant) -or -not (Test-Path $simulator)) {
    Write-Error "请先运行 .\scripts\publish-framework-dependent.ps1 生成 ZIP。"
}

$notes = @"
## 运行要求
安装 [.NET 8 桌面运行时](https://dotnet.microsoft.com/download/dotnet/8.0)（Windows x64）。

## 文件
- **SerialPortAssistant-win-x64-fd.zip** — 串口助手（解压后运行 SerialPortAssistant.exe）
- **SerialPortSimulator-win-x64-fd.zip** — 下位机模拟器（解压后运行 SerialPortSimulator.exe）

本 Release **不包含** .NET 运行时，安装包体积较小。
"@

& $gh release create $Tag $assistant $simulator `
    --repo vikyzhong/SerialPortAssistant `
    --title $Title `
    --notes $notes

Write-Host "Release created: https://github.com/vikyzhong/SerialPortAssistant/releases/tag/$Tag"
