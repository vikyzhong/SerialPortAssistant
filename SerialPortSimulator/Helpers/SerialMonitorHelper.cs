namespace SerialPortSimulator.Helpers;

public static class SerialMonitorHelper
{
    public static bool TryTakeMessage(List<byte> buffer, out byte[] message)
    {
        message = Array.Empty<byte>();

        for (var i = 0; i < buffer.Count; i++)
        {
            if (buffer[i] != (byte)'\n')
                continue;

            var end = i;
            if (end > 0 && buffer[end - 1] == (byte)'\r')
                end--;

            message = buffer.Take(end).ToArray();
            buffer.RemoveRange(0, i + 1);
            return true;
        }

        return false;
    }

    public static byte[] ParseHexPayload(string text)
    {
        var parts = text.Split([' ', ',', '-', '\t', '\r', '\n'], StringSplitOptions.RemoveEmptyEntries);
        if (parts.Length == 0)
            return Array.Empty<byte>();

        var bytes = new byte[parts.Length];
        for (var i = 0; i < parts.Length; i++)
            bytes[i] = Convert.ToByte(parts[i], 16);

        return bytes;
    }

    public static string FormatHex(byte[] data) =>
        data.Length == 0 ? string.Empty : string.Join(' ', data.Select(b => b.ToString("X2")));

    public static string FormatText(byte[] data)
    {
        var sb = new System.Text.StringBuilder(data.Length);
        foreach (var b in data)
        {
            sb.Append(b switch
            {
                (byte)'\r' => "\\r",
                (byte)'\n' => "\\n",
                (byte)'\t' => "\\t",
                >= 32 and <= 126 => (char)b,
                _ => $"\\x{b:X2}"
            });
        }

        return sb.ToString();
    }
}
