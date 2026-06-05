using System.Text;
using SerialPortAssistant.Helpers;
using SerialPortAssistant.Models;

namespace SerialPortAssistant.Models;

/// <summary>命令区实际发送过的报文快照（持久化）。</summary>
public sealed class SentCommandHistoryItem
{
    public DateTime SentAt { get; set; }
    public string DisplayLabel { get; set; } = string.Empty;
    public bool UsedBinaryWire { get; set; }
    public string? TextLine { get; set; }
    /// <summary>完整帧 HEX（空格分隔），二进制发送时使用。</summary>
    public string? RawFrameHex { get; set; }

    public static SentCommandHistoryItem FromBundle(CommandSendBundle bundle)
    {
        var item = new SentCommandHistoryItem
        {
            SentAt = DateTime.Now,
            UsedBinaryWire = bundle.UsedBinaryWire,
            TextLine = bundle.TextLine,
            RawFrameHex = bundle.RawFrame != null
                ? TrafficFormatter.FormatHex(bundle.RawFrame)
                : null
        };
        item.DisplayLabel = BuildDisplayLabel(item);
        return item;
    }

    public static string BuildDisplayLabel(SentCommandHistoryItem item)
    {
        var time = item.SentAt.ToString("HH:mm:ss");
        if (!string.IsNullOrEmpty(item.TextLine))
        {
            var line = item.TextLine.TrimEnd('\r', '\n');
            if (line.Length > 36)
                line = line[..36] + "…";
            return $"{time} · {line}";
        }

        if (!string.IsNullOrEmpty(item.RawFrameHex))
        {
            var hex = item.RawFrameHex.Trim();
            if (hex.Length > 32)
                hex = hex[..32] + "…";
            return $"{time} · {hex}";
        }

        return $"{time} · (空)";
    }

    public bool Matches(SentCommandHistoryItem other)
    {
        if (UsedBinaryWire != other.UsedBinaryWire)
            return false;
        if (!string.IsNullOrEmpty(TextLine) && !string.IsNullOrEmpty(other.TextLine))
            return string.Equals(
                TextLine.TrimEnd('\r', '\n'),
                other.TextLine.TrimEnd('\r', '\n'),
                StringComparison.Ordinal);
        if (!string.IsNullOrEmpty(RawFrameHex) && !string.IsNullOrEmpty(other.RawFrameHex))
            return string.Equals(
                NormalizeHex(RawFrameHex),
                NormalizeHex(other.RawFrameHex),
                StringComparison.OrdinalIgnoreCase);
        return false;
    }

    private static string NormalizeHex(string hex) =>
        string.Join(' ', hex.Split([' ', ',', '\t'], StringSplitOptions.RemoveEmptyEntries)).Trim();
}
