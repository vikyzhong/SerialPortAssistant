namespace SerialPortAssistant.Models;

/// <summary>当前选中命令的文本帧与二进制帧预览及发送数据。</summary>
public sealed class CommandDraft
{
  public required string TextBody { get; init; }
  public required string PayloadHex { get; init; }
  public required string TextFrameDisplay { get; init; }
  public required string BinaryFrameHex { get; init; }
  public required byte[] TextFrameBytes { get; init; }
  public required byte[] BinaryFrame { get; init; }
  public required string ResolvedHint { get; init; }
  public int BinaryByteCount => BinaryFrame.Length;
}
