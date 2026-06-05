namespace SerialPortAssistant.Models;

/// <summary>仅 AT+ 文本命令通道的行结束符；DATA/LOG/CMD/ACK 等仍使用 LF。</summary>
public enum AtTextLineEnding
{
    Lf = 0,
    CrLf = 1
}
