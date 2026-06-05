using SerialPortAssistant.Helpers;
using SerialPortAssistant.Models;

namespace SerialPortAssistant.Services;

public sealed class LineParser
{
    private readonly Func<AppSettings> _getSettings;

    public LineParser(Func<AppSettings> getSettings) => _getSettings = getSettings;

    public ParsedLine Parse(string line)
    {
        if (string.IsNullOrWhiteSpace(line))
            return new ParsedLine(LineKind.Raw, string.Empty);

        var settings = _getSettings();
        var prefixes = settings.Prefixes;

        if (StartsWithPrefix(line, prefixes.Data))
            return ParseData(line[prefixes.Data.Length..]);

        if (StartsWithPrefix(line, prefixes.Log))
            return ParseLog(line[prefixes.Log.Length..], LogLevelConfig.Normalize(settings.LogLevels));

        if (AtLineClassifier.TryParseAsTextResponse(line, out var ackPayload))
            return new ParsedLine(LineKind.Ack, ackPayload);

        if (StartsWithPrefix(line, prefixes.Cmd))
            return new ParsedLine(LineKind.Cmd, line[prefixes.Cmd.Length..].Trim());

        return new ParsedLine(LineKind.Raw, line);
    }

    private static ParsedLine ParseData(string payload)
    {
        var parts = payload.Split([',', ' ', '\t'], StringSplitOptions.RemoveEmptyEntries);
        var values = new List<ushort>();

        foreach (var part in parts)
        {
            if (!TryParseUInt16(part, out var v))
                return new ParsedLine(LineKind.Raw, $"DATA:{payload}");

            values.Add(v);
            if (values.Count >= DataChannels.MaxCount)
                break;
        }

        if (values.Count == 0)
            return new ParsedLine(LineKind.Raw, $"DATA:{payload}");

        return new ParsedLine(LineKind.Data, values.ToArray());
    }

    /// <summary>
    /// LOG: 后第一字节为等级 (0~4)，其余为消息文本。
    /// 亦支持 ASCII 数字 0~4 作为等级（如 LOG:2,消息 或 LOG:2消息）。
    /// </summary>
    private static ParsedLine ParseLog(string payload, List<LogLevelConfig> levels)
    {
        if (string.IsNullOrEmpty(payload))
            return new ParsedLine(LineKind.Log, string.Empty, 0, levels[0].Name);

        int levelId;
        string message;

        var first = payload[0];
        if (first <= LogLevels.MaxLevelId)
        {
            levelId = first;
            message = payload.Length > 1 ? payload[1..].TrimStart(',', ' ', '\t', ':') : string.Empty;
        }
        else if (first is >= '0' and <= '4' && payload.Length >= 1)
        {
            levelId = first - '0';
            message = payload.Length > 1 ? payload[1..].TrimStart(',', ' ', '\t', ':') : string.Empty;
        }
        else
        {
            return new ParsedLine(LineKind.Raw, $"LOG:{payload}");
        }

        var config = levels.FirstOrDefault(l => l.LevelId == levelId) ?? levels[levelId];
        return new ParsedLine(LineKind.Log, message, levelId, config.Name);
    }

    private static bool StartsWithPrefix(string line, string prefix) =>
        !string.IsNullOrEmpty(prefix) &&
        line.StartsWith(prefix, StringComparison.OrdinalIgnoreCase);

    private static bool TryParseUInt16(string text, out ushort value)
    {
        value = 0;
        text = text.Trim();
        if (text.StartsWith("0x", StringComparison.OrdinalIgnoreCase))
            return ushort.TryParse(text[2..], System.Globalization.NumberStyles.HexNumber, null, out value);

        if (!int.TryParse(text, out var n) || n < 0 || n > 65535)
            return false;

        value = (ushort)n;
        return true;
    }
}
