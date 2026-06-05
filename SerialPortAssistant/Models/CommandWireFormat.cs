namespace SerialPortAssistant.Models;

/// <summary>命令线上编码方式（与「命令名 / 字节」编辑区分离）。</summary>
public enum CommandWireFormat
{
  /// <summary>已废弃，加载时迁移为 Text。勿在 UI 使用。</summary>
  Auto = 0,
  /// <summary>AT+ + 命令名（文本/字符串发送）。</summary>
  Text = 1,
  /// <summary>CMD + 字节区 payload。</summary>
  Binary = 2
}
