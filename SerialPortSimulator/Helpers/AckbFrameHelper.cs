using System.Text;

namespace SerialPortSimulator.Helpers;

public static class AckbFrameHelper
{
    public const byte StatusOk = 0;
    public const byte StatusErr = 1;

    public static byte[] Build(byte opcode, byte status, ReadOnlySpan<byte> extra = default, string prefix = "ACK")
    {
        Span<byte> payload = stackalloc byte[2 + extra.Length];
        payload[0] = opcode;
        payload[1] = status;
        extra.CopyTo(payload[2..]);

        var prefixBytes = Encoding.UTF8.GetBytes(prefix);
        var frame = new byte[prefixBytes.Length + 1 + payload.Length + 1];
        prefixBytes.CopyTo(frame.AsSpan());
        frame[prefixBytes.Length] = (byte)payload.Length;
        payload.CopyTo(frame.AsSpan(prefixBytes.Length + 1));
        frame[^1] = (byte)'\n';
        return frame;
    }
}
