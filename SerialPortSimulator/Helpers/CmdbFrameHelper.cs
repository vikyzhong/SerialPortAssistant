using System.Text;

namespace SerialPortSimulator.Helpers;

public static class CmdbFrameHelper
{
    public const int MaxPayloadLength = 16;

    public static bool TryParseFrame(
        IReadOnlyList<byte> buffer,
        string prefix,
        out byte[] payload,
        out int consumed)
    {
        payload = [];
        consumed = 0;

        var prefixBytes = Encoding.UTF8.GetBytes(prefix);
        if (prefixBytes.Length == 0 || buffer.Count < prefixBytes.Length)
            return false;

        for (var i = 0; i < prefixBytes.Length; i++)
        {
            if (!EqualsIgnoreCase(buffer[i], prefixBytes[i]))
                return false;
        }

        var prefixLen = prefixBytes.Length;
        if (buffer.Count < prefixLen + 1)
            return false;

        var payloadLen = buffer[prefixLen];
        if (payloadLen is < 1 or > MaxPayloadLength)
        {
            consumed = 1;
            payload = [];
            return true;
        }

        var frameLen = prefixLen + 1 + payloadLen + 1;
        if (buffer.Count < frameLen)
            return false;

        if (buffer[frameLen - 1] != (byte)'\n')
        {
            consumed = 1;
            payload = [];
            return true;
        }

        payload = buffer.Skip(prefixLen + 1).Take(payloadLen).ToArray();
        consumed = frameLen;
        return true;
    }

    private static bool EqualsIgnoreCase(byte a, byte b)
    {
        if (a == b) return true;
        if (a is >= (byte)'A' and <= (byte)'Z') return b == a + 32;
        if (b is >= (byte)'A' and <= (byte)'Z') return a == b + 32;
        return false;
    }
}
