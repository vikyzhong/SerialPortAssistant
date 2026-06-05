using System.IO.Ports;

namespace SerialPortAssistant.Models;

/// <summary>串口连接参数（打开前配置，写入 settings.json）。</summary>
public sealed class SerialPortPreferences
{
    public Handshake Handshake { get; set; } = Handshake.None;
    public bool DtrEnable { get; set; }
    public bool RtsEnable { get; set; }
}
