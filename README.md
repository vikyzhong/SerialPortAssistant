# 串口助手 (Serial Port Assistant) **V0.01**

Windows WPF 串口助手，支持最高 **1,000,000** 波特率。

## 布局

- **上方**：串口连接栏
- **中部左**：实时数据曲线（最多 3 路） | **中部右**：日志
- **下方**：AT+ 命令发送 → **其下** 命令回传（与中部左右对称）

## 协议格式

命令类前缀均为 **3 字节**，便于下位机定长判头（与 `AT+` 一致）：

| 前缀 / 形式 | 示例 | 行为 |
|------|------|------|
| `DATA:` | `DATA:1000,2000,3000` | 1~3 路无符号 **16 位** 整数 (0-65535)，ASCII 十进制/十六进制文本 |
| `DATB:` | 二进制帧 | **高速**：`DATB:` + count(1~3) + count×uint16 **大端** + `\n` |
| `LOG:` | 首字节等级 `0~4` + 消息 | 5 级固定；主界面勾选显示哪些等级 |
| `AT+` | `AT+GMR` / `AT+RST` 等 | 主机 → 下位机，ASCII + **CRLF**（设置可改 LF） |
| `CMD` | 二进制帧 | 主机 → 下位机：**CMD(3) + len + opcode + 参数 + LF** |
| `OK` / `ERROR` | `OK` / `ERROR` 等 | 下位机 → 主机，AT 风格文本回传 + LF |
| `ACK` | 二进制帧 | 下位机 → 主机：**ACK(3) + len + opcode + status(0/1) + LF** |

**对称约定**

| 方向 | 文本（3 字节头） | 二进制（3 字节头） |
|------|------------------|---------------------|
| 上位机 → 下位机 | `AT+` + 正文 + **CRLF** | `CMD` + len + payload + **LF** |
| 下位机 → 上位机 | `OK` / `ERR` / `+名称:值` 等行 + LF | `ACK` + len + opcode + status + LF |

**判头 HEX**：`AT+` → `41 54 2B`　`CMD` → `43 4D 44`　`ACK` → `41 43 4B`

通道名称（默认 X/Y/Z）可在 **配置 → 数据通道** 中修改。

## 下载运行（Release）

GitHub [Releases](https://github.com/vikyzhong/SerialPortAssistant/releases) 提供 **框架依赖** 版（体积小，**不包含** .NET 运行时）：

| 文件 | 说明 |
|------|------|
| `SerialPortAssistant-win-x64-fd.zip` | 串口助手 |
| `SerialPortSimulator-win-x64-fd.zip` | 下位机模拟器 |

**使用前请安装 [.NET 8 桌面运行时](https://dotnet.microsoft.com/download/dotnet/8.0)**（Windows x64，Desktop Runtime）。解压 ZIP 后运行其中的 `.exe` 即可。

自行打包框架依赖版：

```powershell
.\scripts\publish-framework-dependent.ps1
```

输出目录：`publish\releases\`。曲线使用 **WPF 自绘**（已移除 ScottPlot），发布包明显更小。若需自带运行时的单文件包（约 70MB），使用发布配置 `Win10-Standalone`（不推荐作默认分发）。

## 构建与运行

```powershell
cd C:\Users\1\Projects\SerialPortAssistant
dotnet build
dotnet run --project SerialPortAssistant
```

## 虚拟串口联调

使用同仓库下的 **SerialPortSimulator**（下位机模拟器）配合 com0com 测试：

```powershell
dotnet run --project SerialPortSimulator
```

详见 [SerialPortSimulator/README.md](SerialPortSimulator/README.md)。

## 联调示例（加速度 3 轴）

```
DATA:16384,16402,16390
LOG:1IMU started
AT+GMR
SerialPortSimulator V0.01-sim
OK
```

## 二进制复位示例

```
主机发 CMD 帧（opcode=0x01）：43 4D 44 01 01 0A
下位机回 ACK 帧（opcode=0x01, status=OK）：41 43 4B 02 01 00 0A
```
