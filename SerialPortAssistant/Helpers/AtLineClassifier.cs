namespace SerialPortAssistant.Helpers;

/// <summary>AT 风格文本回传行分类（无 ACKS: 前缀）。</summary>
public static class AtLineClassifier
{
    public static bool TryParseAsTextResponse(string line, out string payload)
    {
        payload = string.Empty;
        if (string.IsNullOrWhiteSpace(line))
            return false;

        line = line.Trim();

        if (line.StartsWith('+') && line.IndexOf(':') > 1)
        {
            payload = line;
            return true;
        }

        if (line.Equals("OK", StringComparison.OrdinalIgnoreCase))
        {
            payload = "OK";
            return true;
        }

        if (line.StartsWith("OK ", StringComparison.OrdinalIgnoreCase) ||
            line.StartsWith("OK\t", StringComparison.OrdinalIgnoreCase))
        {
            payload = line;
            return true;
        }

        if (line.StartsWith("ERROR", StringComparison.OrdinalIgnoreCase) ||
            line.StartsWith("ERR ", StringComparison.OrdinalIgnoreCase) ||
            line.Equals("ERR", StringComparison.OrdinalIgnoreCase))
        {
            payload = line;
            return true;
        }

        if (line.StartsWith("NAK", StringComparison.OrdinalIgnoreCase))
        {
            payload = line;
            return true;
        }

        return false;
    }
}
