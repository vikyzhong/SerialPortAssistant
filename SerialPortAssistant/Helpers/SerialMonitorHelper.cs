namespace SerialPortAssistant.Helpers;

public static class SerialMonitorHelper
{
    /// <summary>从缓冲区提取以换行结尾的报文；返回 true 表示取到一条（可为空报文）。</summary>
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
            var removeCount = i + 1;
            buffer.RemoveRange(0, removeCount);
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
        {
            var token = parts[i].Trim();
            if (token.StartsWith("0x", StringComparison.OrdinalIgnoreCase))
                token = token[2..];
            if (token.StartsWith('#'))
                token = token[1..];
            bytes[i] = Convert.ToByte(token, 16);
        }

        return bytes;
    }
}
