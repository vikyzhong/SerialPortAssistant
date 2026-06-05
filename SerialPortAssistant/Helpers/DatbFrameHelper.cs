using System.Text;
using SerialPortAssistant.Models;

namespace SerialPortAssistant.Helpers;

public static class DatbFrameHelper
{
    public static byte[] Build(ReadOnlySpan<ushort> channels, string prefix = "DATB:")
    {
        if (channels.Length is < 1 or > DataChannels.MaxCount)
            throw new ArgumentOutOfRangeException(nameof(channels), "通道数须为 1~3。");

        var prefixBytes = Encoding.UTF8.GetBytes(prefix);
        var frame = new byte[prefixBytes.Length + 1 + channels.Length * 2 + 1];
        prefixBytes.CopyTo(frame.AsSpan());
        frame[prefixBytes.Length] = (byte)channels.Length;

        var offset = prefixBytes.Length + 1;
        for (var i = 0; i < channels.Length; i++)
        {
            frame[offset++] = (byte)(channels[i] >> 8);
            frame[offset++] = (byte)(channels[i] & 0xFF);
        }

        frame[^1] = (byte)'\n';
        return frame;
    }

    public static ushort ReadUInt16BigEndian(byte hi, byte lo) => (ushort)((hi << 8) | lo);
}
