using System.Text;

namespace SerialPortAssistant.Helpers;

public static class CmdbFrameHelper
{
    public const int MaxPayloadLength = 16;

    public static byte[] Build(ReadOnlySpan<byte> payload, string prefix = "CMD")
    {
        if (payload.Length is < 1 or > MaxPayloadLength)
            throw new ArgumentOutOfRangeException(nameof(payload), $"CMD payload 长度须为 1~{MaxPayloadLength}。");

        var prefixBytes = Encoding.UTF8.GetBytes(prefix);
        var frame = new byte[prefixBytes.Length + 1 + payload.Length + 1];
        prefixBytes.CopyTo(frame.AsSpan());
        frame[prefixBytes.Length] = (byte)payload.Length;
        payload.CopyTo(frame.AsSpan(prefixBytes.Length + 1));
        frame[^1] = (byte)'\n';
        return frame;
    }

    public static byte[] BuildOpcode(byte opcode, ReadOnlySpan<byte> args = default, string prefix = "CMD")
    {
        Span<byte> payload = stackalloc byte[1 + args.Length];
        payload[0] = opcode;
        args.CopyTo(payload[1..]);
        return Build(payload, prefix);
    }
}
