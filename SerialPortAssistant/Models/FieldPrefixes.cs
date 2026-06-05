namespace SerialPortAssistant.Models;

public sealed class FieldPrefixes
{
    public string Data { get; set; } = "DATA:";
    /// <summary>二进制 DATA：前缀 + count(1~3) + count×uint16 大端 + LF。</summary>
    public string DataBinary { get; set; } = "DATB:";
    public string Log { get; set; } = "LOG:";
    /// <summary>文本命令前缀（默认 AT+）。</summary>
    public string Cmd { get; set; } = "AT+";
    /// <summary>二进制 CMD：前缀(3) + len + payload(len) + LF，payload[0]=opcode。</summary>
    public string CmdBinary { get; set; } = "CMD";
    /// <summary>二进制 ACK：前缀(3) + len + payload(len) + LF，payload[0]=opcode,payload[1]=status。</summary>
    public string AckBinary { get; set; } = "ACK";
}
