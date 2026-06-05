using System.Text.Json.Serialization;
using SerialPortAssistant.Helpers;

namespace SerialPortAssistant.Models;

public sealed class CommandOpcodeDefinition
{
    public byte Opcode { get; set; }
    public string Name { get; set; } = "命令";
    /// <summary>AT+ 文本别名，如 GMR → AT+GMR。</summary>
    public string? TextAlias { get; set; }
    /// <summary>CMD 二进制 payload（含 opcode），如 0x02。</summary>
    public string? DefaultPayloadHex { get; set; }

    [JsonIgnore]
    public string OpcodeText
    {
        get => OpcodeFormat.Format(Opcode);
        set
        {
            if (OpcodeFormat.TryParse(value, out var parsed))
                Opcode = parsed;
        }
    }

    [JsonIgnore]
    public string DisplayLabel =>
      string.IsNullOrEmpty(TextAlias)
        ? $"{OpcodeFormat.Format(Opcode)} {Name}"
        : $"{OpcodeFormat.Format(Opcode)} {Name} · {TextAlias}";

    /// <summary>默认 AT+ 命令表（常见模组/ESP 风格命名，可按需增删）。</summary>
    public static List<CommandOpcodeDefinition> CreateDefaults() =>
    [
        new() { Opcode = 0x01, Name = "复位", TextAlias = "RST", DefaultPayloadHex = "0x01" },
        new() { Opcode = 0x02, Name = "读版本", TextAlias = "GMR", DefaultPayloadHex = "0x02" },
        new() { Opcode = 0x03, Name = "恢复出厂", TextAlias = "RESTORE", DefaultPayloadHex = "0x03" },
        new() { Opcode = 0x04, Name = "深度睡眠", TextAlias = "GSLP", DefaultPayloadHex = "0x04" },
        new() { Opcode = 0x05, Name = "回显", TextAlias = "ECHO", DefaultPayloadHex = "0x05" },
        new() { Opcode = 0x06, Name = "查询 UART", TextAlias = "UART_CUR?", DefaultPayloadHex = "0x06" },
        new() { Opcode = 0x07, Name = "查询 WiFi 模式", TextAlias = "CWMODE?", DefaultPayloadHex = "0x07" },
        new() { Opcode = 0x08, Name = "扫描热点", TextAlias = "CWLAP", DefaultPayloadHex = "0x08" },
        new() { Opcode = 0x09, Name = "查询已连 AP", TextAlias = "CWJAP?", DefaultPayloadHex = "0x09" },
        new() { Opcode = 0x0A, Name = "Ping", TextAlias = "PING", DefaultPayloadHex = "0x0A" },
        new() { Opcode = 0x0B, Name = "连接状态", TextAlias = "CIPSTATUS", DefaultPayloadHex = "0x0B" },
        new() { Opcode = 0x0C, Name = "保存配置", TextAlias = "SAVE", DefaultPayloadHex = "0x0C" },
        new() { Opcode = 0x0D, Name = "关闭回显", TextAlias = "ATE0", DefaultPayloadHex = "0x0D" },
        new() { Opcode = 0x0E, Name = "开启回显", TextAlias = "ATE1", DefaultPayloadHex = "0x0E" }
    ];
}
