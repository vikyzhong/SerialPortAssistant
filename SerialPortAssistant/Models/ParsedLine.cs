namespace SerialPortAssistant.Models;



public sealed class ParsedLine

{

    public LineKind Kind { get; }

    public ushort[] Channels { get; }

    public string Text { get; }

    /// <summary>LOG 等级字节 0~4。</summary>

    public int LogLevelId { get; }

    public string? LogLevelName { get; }

    /// <summary>ACK/CMD 首字节 opcode；-1 表示无。</summary>

    public int BinaryOpcode { get; init; } = -1;

    /// <summary>ACK status：0=OK,1=ERR；-1 表示无。</summary>

    public int BinaryStatus { get; init; } = -1;

    public byte[] BinaryPayload { get; init; } = [];



    public ParsedLine(LineKind kind, ushort[] channels)

    {

        Kind = kind;

        Channels = channels;

        Text = string.Empty;

    }



    public ParsedLine(LineKind kind, string text, int logLevelId = 0, string? logLevelName = null)

    {

        Kind = kind;

        Text = text;

        LogLevelId = logLevelId;

        LogLevelName = logLevelName;

        Channels = [];

    }



    public ParsedLine(

        LineKind kind,

        string text,

        byte opcode,

        byte status,

        byte[] payload)

    {

        Kind = kind;

        Text = text;

        BinaryOpcode = opcode;

        BinaryStatus = status;

        BinaryPayload = payload;

        Channels = [];

    }

}


