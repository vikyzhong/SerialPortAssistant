namespace SerialPortAssistant.Helpers;

/// <summary>CMD payload 编辑区：空格分隔、大写、无 0x 前缀。</summary>
public static class HexPayloadFormat
{
    public static string FormatBytes(ReadOnlySpan<byte> bytes) =>
        bytes.Length == 0 ? string.Empty : TrafficFormatter.FormatHex(bytes.ToArray());

    public static string FormatPayload(byte opcode, ReadOnlySpan<byte> args)
    {
        if (args.Length == 0)
            return FormatBytes([opcode]);

        Span<byte> payload = stackalloc byte[1 + args.Length];
        payload[0] = opcode;
        args.CopyTo(payload[1..]);
        return FormatBytes(payload);
    }

    /// <summary>编辑时自动识别 HEX：去 0x/分隔符，按字节加空格（末尾可留半字节）。</summary>
    public static string FormatInputLive(string? input)
    {
        if (string.IsNullOrWhiteSpace(input))
            return string.Empty;

        var digits = new System.Text.StringBuilder();
        var text = input.AsSpan();
        for (var i = 0; i < text.Length; i++)
        {
            var c = text[i];
            if (char.IsWhiteSpace(c) || c is ',' or '-' or '\t')
                continue;

            if (c == '0' && i + 1 < text.Length && (text[i + 1] is 'x' or 'X'))
            {
                i++;
                continue;
            }

            if (!Uri.IsHexDigit(c))
                continue;

            digits.Append(char.ToUpperInvariant(c));
        }

        if (digits.Length == 0)
            return input.Trim();

        var grouped = new System.Text.StringBuilder();
        for (var i = 0; i < digits.Length; i += 2)
        {
            if (grouped.Length > 0)
                grouped.Append(' ');

            if (i + 1 < digits.Length)
                grouped.Append(digits[i]).Append(digits[i + 1]);
            else
                grouped.Append(digits[i]);
        }

        return grouped.ToString();
    }

    /// <summary>末尾是否为未输完的半字节（如 "1"、"12 7"），此时勿按整字节解析以免自动补 0。</summary>
    public static bool HasIncompleteTrailingNibble(string? formatted)
    {
        if (string.IsNullOrWhiteSpace(formatted))
            return false;

        var digitCount = 0;
        foreach (var c in formatted)
        {
            if (Uri.IsHexDigit(c))
                digitCount++;
        }

        return digitCount % 2 == 1;
    }

    /// <summary>解析输入并规范为「02 0A」形式；失败则返回 false。</summary>
    public static bool TryNormalizeDisplay(string? input, out string display)
    {
        display = string.Empty;
        if (string.IsNullOrWhiteSpace(input))
            return false;

        try
        {
            var bytes = SerialMonitorHelper.ParseHexPayload(input);
            if (bytes.Length == 0)
                return false;

            display = FormatBytes(bytes);
            return true;
        }
        catch
        {
            return false;
        }
    }
}
