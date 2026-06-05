using System.Text;
using SerialPortAssistant.Helpers;
using SerialPortAssistant.Models;

namespace SerialPortAssistant.Helpers;

public enum SentCommandRestoreKind
{
    Cmds,
    Cmdb,
    Raw
}

public static class SentCommandHistoryRestorer
{
    public static SentCommandRestoreKind TryRestore(
        SentCommandHistoryItem item,
        FieldPrefixes prefixes,
        out string textBody,
        out string payloadHex,
        out string rawSendHex)
    {
        textBody = string.Empty;
        payloadHex = string.Empty;
        rawSendHex = string.Empty;

        if (!string.IsNullOrEmpty(item.TextLine))
        {
            var line = item.TextLine.TrimEnd('\r', '\n');
            if (StartsWithCmdPrefix(line, prefixes.Cmd))
            {
                textBody = StripCmdPrefix(line, prefixes.Cmd);
                return SentCommandRestoreKind.Cmds;
            }

            rawSendHex = TrafficFormatter.FormatHex(Encoding.UTF8.GetBytes(line));
            if (!line.EndsWith('\n'))
                rawSendHex += " 0A";
            return SentCommandRestoreKind.Raw;
        }

        if (!string.IsNullOrEmpty(item.RawFrameHex))
        {
            byte[] frame;
            try
            {
                frame = SerialMonitorHelper.ParseHexPayload(item.RawFrameHex);
            }
            catch
            {
                rawSendHex = item.RawFrameHex;
                return SentCommandRestoreKind.Raw;
            }

            if (TryParseCmdbPayload(frame, prefixes.CmdBinary, out payloadHex))
                return SentCommandRestoreKind.Cmdb;

            rawSendHex = TrafficFormatter.FormatHex(frame);
            return SentCommandRestoreKind.Raw;
        }

        return SentCommandRestoreKind.Raw;
    }

    public static bool TryParseCmdbPayload(byte[] frame, string prefix, out string payloadHex)
    {
        payloadHex = string.Empty;
        var prefixBytes = Encoding.UTF8.GetBytes(string.IsNullOrEmpty(prefix) ? "CMD" : prefix);
        if (frame.Length < prefixBytes.Length + 2)
            return false;
        if (!StartsWith(frame, prefixBytes))
            return false;

        var len = frame[prefixBytes.Length];
        var payloadStart = prefixBytes.Length + 1;
        if (len < 1 || payloadStart + len > frame.Length)
            return false;

        var payload = frame.AsSpan(payloadStart, len);
        payloadHex = string.Join(' ', payload.ToArray().Select(b => b.ToString("X2")));
        return true;
    }

    private static bool StartsWithCmdPrefix(string line, string prefix) =>
        !string.IsNullOrEmpty(prefix) &&
        line.StartsWith(prefix, StringComparison.OrdinalIgnoreCase);

    private static string StripCmdPrefix(string line, string prefix)
    {
        if (StartsWithCmdPrefix(line, prefix))
            return line[prefix.Length..].TrimStart();
        return line;
    }

    private static bool StartsWith(byte[] buffer, byte[] prefix)
    {
        if (buffer.Length < prefix.Length)
            return false;
        for (var i = 0; i < prefix.Length; i++)
        {
            if (buffer[i] != prefix[i])
                return false;
        }

        return true;
    }
}
