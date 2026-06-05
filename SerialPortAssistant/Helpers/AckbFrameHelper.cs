using System.Text;

namespace SerialPortAssistant.Helpers;

public static class AckbFrameHelper
{
    public const int MaxPayloadLength = 16;
    public const byte StatusOk = 0;
    public const byte StatusErr = 1;

    public static byte[] Build(ReadOnlySpan<byte> payload, string prefix = "ACK")
    {
        if (payload.Length is < 2 or > MaxPayloadLength)
            throw new ArgumentOutOfRangeException(nameof(payload), $"ACK payload 长度须为 2~{MaxPayloadLength}。");

        var prefixBytes = Encoding.UTF8.GetBytes(prefix);
        var frame = new byte[prefixBytes.Length + 1 + payload.Length + 1];
        prefixBytes.CopyTo(frame.AsSpan());
        frame[prefixBytes.Length] = (byte)payload.Length;
        payload.CopyTo(frame.AsSpan(prefixBytes.Length + 1));
        frame[^1] = (byte)'\n';
        return frame;
    }

    public static byte[] BuildOpcode(byte opcode, byte status, ReadOnlySpan<byte> extra = default, string prefix = "ACK")
    {
        Span<byte> payload = stackalloc byte[2 + extra.Length];
        payload[0] = opcode;
        payload[1] = status;
        extra.CopyTo(payload[2..]);
        return Build(payload, prefix);
    }
}
