using System.IO.Ports;

namespace SerialPortAssistant.Models;

public sealed class SerialSettings
{
    public string PortName { get; set; } = "COM1";
    public int BaudRate { get; set; } = 115200;
    public Parity Parity { get; set; } = Parity.None;
    public int DataBits { get; set; } = 8;
    public StopBits StopBits { get; set; } = StopBits.One;
    public Handshake Handshake { get; set; } = Handshake.None;
    public bool DtrEnable { get; set; }
    public bool RtsEnable { get; set; }

    public static IReadOnlyList<int> PresetBaudRates { get; } =
    [
        9600, 19200, 38400, 57600, 115200, 230400, 460800, 921600, 1000000
    ];
}
