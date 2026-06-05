using System.Text.Json.Serialization;

namespace SerialPortAssistant.Models;

public sealed class CommandPreset
{
  public string Name { get; set; } = "新命令";
  public CommandWireFormat WireFormat { get; set; } = CommandWireFormat.Text;
  /// <summary>命令名或文本别名，如 GMR。</summary>
  public string Payload { get; set; } = "GMR";
  /// <summary>CMD payload：opcode + 参数字节。</summary>
  public string HexPayload { get; set; } = "02";

  /// <summary>旧版 sendMode，仅用于 settings 迁移。</summary>
  [JsonPropertyName("sendMode")]
  public CommandSendMode? LegacySendMode { get; set; }

  public void ApplyLegacyMigration()
  {
    if (WireFormat == CommandWireFormat.Auto)
      WireFormat = CommandWireFormat.Text;

    if (LegacySendMode == CommandSendMode.Hex)
      WireFormat = CommandWireFormat.Binary;

    LegacySendMode = null;
  }
}
