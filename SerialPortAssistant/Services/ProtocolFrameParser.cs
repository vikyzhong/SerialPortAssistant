using System.Text;
using SerialPortAssistant.Helpers;
using SerialPortAssistant.Models;

namespace SerialPortAssistant.Services;

/// <summary>
/// 字节流解复用：DATB/ACK 二进制帧 + 以 LF 结尾的文本协议行。
/// </summary>
public sealed class ProtocolFrameParser
{
    private readonly List<byte> _buffer = new(4096);
    private readonly Func<AppSettings> _getSettings;
    private LineParser? _lineParser;

    public ProtocolFrameParser(Func<AppSettings> getSettings) => _getSettings = getSettings;

    public void Configure(LineParser lineParser) => _lineParser = lineParser;

    public void Clear() => _buffer.Clear();

    public IEnumerable<ParsedLine> Append(ReadOnlySpan<byte> chunk)
    {
        if (chunk.Length > 0)
            _buffer.AddRange(chunk);

        var results = new List<ParsedLine>();

        while (_buffer.Count > 0)
        {
            if (TryConsumeDatbFrame(out var datbLine, out var datbConsumed))
            {
                if (datbConsumed <= 0)
                    break;

                _buffer.RemoveRange(0, datbConsumed);
                if (datbLine != null)
                    results.Add(datbLine);
                continue;
            }

            if (TryConsumeAckbFrame(out var ackbLine, out var ackbConsumed))
            {
                if (ackbConsumed <= 0)
                    break;

                _buffer.RemoveRange(0, ackbConsumed);
                if (ackbLine != null)
                    results.Add(ackbLine);
                continue;
            }

            if (TryConsumeTextLine(out var textLine, out var textConsumed))
            {
                _buffer.RemoveRange(0, textConsumed);
                if (textLine != null)
                    results.Add(textLine);
                continue;
            }

            break;
        }

        return results;
    }

    private bool TryConsumeDatbFrame(out ParsedLine? line, out int consumed)
    {
        line = null;
        consumed = 0;

        var prefix = Encoding.UTF8.GetBytes(_getSettings().Prefixes.DataBinary);
        if (prefix.Length == 0 || !StartsWithPrefix(_buffer, prefix))
            return false;

        var prefixLen = prefix.Length;
        if (_buffer.Count < prefixLen + 1)
            return false;

        var count = _buffer[prefixLen];
        if (count is < 1 or > DataChannels.MaxCount)
        {
            consumed = 1;
            return true;
        }

        var frameLen = prefixLen + 1 + count * 2 + 1;
        if (_buffer.Count < frameLen)
            return false;

        if (_buffer[frameLen - 1] != (byte)'\n')
        {
            consumed = 1;
            return true;
        }

        var channels = new ushort[count];
        var offset = prefixLen + 1;
        for (var i = 0; i < count; i++)
        {
            channels[i] = DatbFrameHelper.ReadUInt16BigEndian(_buffer[offset], _buffer[offset + 1]);
            offset += 2;
        }

        line = new ParsedLine(LineKind.Data, channels);
        consumed = frameLen;
        return true;
    }

    private bool TryConsumeAckbFrame(out ParsedLine? line, out int consumed)
    {
        line = null;
        consumed = 0;

        var settings = _getSettings();
        var prefix = Encoding.UTF8.GetBytes(settings.Prefixes.AckBinary);
        if (prefix.Length == 0 || !StartsWithPrefix(_buffer, prefix))
            return false;

        var prefixLen = prefix.Length;
        if (_buffer.Count < prefixLen + 1)
            return false;

        var payloadLen = _buffer[prefixLen];
        if (payloadLen is < 2 or > AckbFrameHelper.MaxPayloadLength)
        {
            consumed = 1;
            return true;
        }

        var frameLen = prefixLen + 1 + payloadLen + 1;
        if (_buffer.Count < frameLen)
            return false;

        if (_buffer[frameLen - 1] != (byte)'\n')
        {
            consumed = 1;
            return true;
        }

        var payload = _buffer.Skip(prefixLen + 1).Take(payloadLen).ToArray();
        var opcode = payload[0];
        var status = payload[1];
        var extra = payload.Length > 2 ? payload[2..] : [];
        var display = CommandFrameEncoder.FormatAckBinaryDisplay(
            opcode, status, extra, settings.CommandOpcodes);

        line = new ParsedLine(LineKind.Ack, display, opcode, status, payload);
        consumed = frameLen;
        return true;
    }

    private bool TryConsumeTextLine(out ParsedLine? line, out int consumed)
    {
        line = null;
        consumed = 0;

        if (CouldBeIncompleteBinaryPrefix())
            return false;

        var newlineIndex = FindNewlineIndex(_buffer);
        if (newlineIndex < 0)
            return false;

        var lineBytes = _buffer.Take(newlineIndex).ToArray();
        var skip = newlineIndex + 1;
        if (skip < _buffer.Count && _buffer[newlineIndex] == (byte)'\r' && _buffer[skip] == (byte)'\n')
            skip++;

        consumed = skip;

        var text = Encoding.UTF8.GetString(lineBytes).Trim();
        if (text.Length == 0)
            return true;

        line = _lineParser?.Parse(text) ?? new ParsedLine(LineKind.Raw, text);
        return true;
    }

    private bool CouldBeIncompleteBinaryPrefix()
    {
        foreach (var prefixText in new[]
                 {
                     _getSettings().Prefixes.DataBinary,
                     _getSettings().Prefixes.AckBinary
                 })
        {
            var prefix = Encoding.UTF8.GetBytes(prefixText);
            if (prefix.Length == 0 || _buffer.Count >= prefix.Length)
                continue;

            if (StartsWithPrefix(_buffer, prefix.AsSpan(0, _buffer.Count)))
                return true;
        }

        return false;
    }

    private static int FindNewlineIndex(IReadOnlyList<byte> buffer)
    {
        for (var i = 0; i < buffer.Count; i++)
        {
            if (buffer[i] is (byte)'\n' or (byte)'\r')
                return i;
        }

        return -1;
    }

    private static bool StartsWithPrefix(IReadOnlyList<byte> buffer, ReadOnlySpan<byte> prefix)
    {
        if (buffer.Count < prefix.Length)
            return false;

        for (var i = 0; i < prefix.Length; i++)
        {
            if (!BytesEqualIgnoreCase(buffer[i], prefix[i]))
                return false;
        }

        return true;
    }

    private static bool BytesEqualIgnoreCase(byte a, byte b)
    {
        if (a == b)
            return true;

        if (a is >= (byte)'A' and <= (byte)'Z')
            return b == a + 32;

        if (b is >= (byte)'A' and <= (byte)'Z')
            return a == b + 32;

        return false;
    }
}
