namespace SerialPortSimulator.Models;



public sealed class CmdRule

{

    public byte? Opcode { get; set; }

    public string CommandKeyword { get; set; } = "GMR";

    public string AckResponse { get; set; } = "OK";

    /// <summary>文本回复前先发的 URC 行（如 +GMR:…）。</summary>
    public string? UrcLine { get; set; }

    /// <summary>AT+GMR 等标准多行正文（在 OK 之前逐行发送）。</summary>
    public List<string>? UrcLines { get; set; }

    public bool UseBinaryAck { get; set; } = true;

    public byte AckStatus { get; set; }

    public bool Enabled { get; set; } = true;

}


