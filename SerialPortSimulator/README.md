# SerialPortSimulator（下位机模拟器）

用于配合 [串口助手](../SerialPortAssistant) 进行虚拟串口联调。

## 默认 AT+ 命令（与助手命令码表一致）

| Opcode | AT+ | 说明 | 模拟器文本回复概要 |
|--------|-----|------|-------------------|
| 0x01 | RST | 复位 | OK |
| 0x02 | GMR | 读版本 | 版本多行 → OK |
| 0x03 | RESTORE | 恢复出厂 | OK |
| 0x04 | GSLP | 深度睡眠 | OK |
| 0x05 | ECHO | 回显 | +ECHO:1 → OK |
| 0x06 | UART_CUR? | 查询 UART | +UART_CUR:115200,8,1,0,0 → OK |
| 0x07 | CWMODE? | WiFi 模式 | +CWMODE:1 → OK |
| 0x08 | CWLAP | 扫描 AP | +CWLAP:… → OK |
| 0x09 | CWJAP? | 已连 AP | +CWJAP:… → OK |
| 0x0A | PING | Ping | OK |
| 0x0B | CIPSTATUS | 连接状态 | STATUS:2 → OK |
| 0x0C | SAVE | 保存配置 | OK |
| 0x0D | ATE0 | 关闭回显 | OK |
| 0x0E | ATE1 | 开启回显 | OK |

未识别 AT+ → `ERROR`；CMD 二进制 → ACK。

联调示例：`AT+GMR` → 版本信息 + `OK`。

```powershell
dotnet run --project SerialPortSimulator
```
