# 上传 GitHub Release（需已安装并登录 gh：gh auth login）
# 用法：.\scripts\upload-github-release.ps1 -Tag v0.02
param(
    [string]$Tag = "v0.02",
    [string]$Title = "V0.02 — 串口助手单文件 (win-x64)"
)

$ErrorActionPreference = "Stop"
$gh = "C:\Program Files\GitHub CLI\gh.exe"
if (-not (Get-Command $gh -ErrorAction SilentlyContinue)) {
    $gh = "gh"
}

$root = Split-Path -Parent (Split-Path -Parent $MyInvocation.MyCommand.Path)
$assistantExe = Join-Path $root "publish\Win10\SerialPortAssistant-SingleFile\SerialPortAssistant.exe"

if (-not (Test-Path $assistantExe)) {
    Write-Error "请先发布：dotnet publish SerialPortAssistant\SerialPortAssistant.csproj -c Release /p:PublishProfile=Win10-SingleFile"
}

$notes = @"
## 运行要求
安装 [.NET 8 桌面运行时](https://dotnet.microsoft.com/download/dotnet/8.0)（Windows x64）。

## 下载
**SerialPortAssistant.exe** — 串口助手单文件（框架依赖，不含 .NET 运行时）。

## V0.02 更新摘要
- 自绘曲线，发布包体积更小
- CMD 载荷 HEX 自动按字节分隔（无 0x 前缀），修复半字节输入被补 0 的问题
- 发送内容可编辑；历史命令发送后保持选中
- AT+「字符串发送」/ CMD「HEX格式发送」
"@

& $gh release create $Tag $assistantExe `
    --repo vikyzhong/SerialPortAssistant `
    --title $Title `
    --notes $notes

Write-Host "Release created: https://github.com/vikyzhong/SerialPortAssistant/releases/tag/$Tag"
