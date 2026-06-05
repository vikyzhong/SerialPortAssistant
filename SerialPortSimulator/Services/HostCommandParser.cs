using System.Text;
using SerialPortSimulator.Helpers;

namespace SerialPortSimulator.Services;

public sealed class HostCommand
{
    public required bool IsBinary { get; init; }
    public byte Opcode { get; init; }
    public byte[] Args { get; init; } = [];
    public string TextLine { get; init; } = string.Empty;
}

public sealed class HostCommandParser
{
    private readonly List<byte> _buffer = new(512);
    private readonly string _cmdBinaryPrefix;

    public HostCommandParser(string cmdBinaryPrefix = "CMD") => _cmdBinaryPrefix = cmdBinaryPrefix;

    public event EventHandler<HostCommand>? CommandReceived;

    public void Append(ReadOnlySpan<byte> chunk)
    {
        if (chunk.Length > 0)
            _buffer.AddRange(chunk);

        while (_buffer.Count > 0)
        {
            if (CmdbFrameHelper.TryParseFrame(_buffer, _cmdBinaryPrefix, out var payload, out var consumed))
            {
                if (consumed <= 0)
                    break;

                _buffer.RemoveRange(0, consumed);
                if (payload.Length > 0)
                {
                    CommandReceived?.Invoke(this, new HostCommand
                    {
                        IsBinary = true,
                        Opcode = payload[0],
                        Args = payload.Length > 1 ? payload[1..] : []
                    });
                }

                continue;
            }

            if (CouldBeIncompleteCmdbPrefix())
                break;

            var newlineIndex = FindNewlineIndex(_buffer);
            if (newlineIndex < 0)
                break;

            var lineBytes = _buffer.Take(newlineIndex).ToArray();
            var skip = newlineIndex + 1;
            if (skip < _buffer.Count && _buffer[newlineIndex] == (byte)'\r' && _buffer[skip] == (byte)'\n')
                skip++;

            _buffer.RemoveRange(0, skip);
            var text = Encoding.UTF8.GetString(lineBytes).Trim();
            if (text.Length == 0)
                continue;

            CommandReceived?.Invoke(this, new HostCommand
            {
                IsBinary = false,
                TextLine = text
            });
        }
    }

    public void Clear() => _buffer.Clear();

    private bool CouldBeIncompleteCmdbPrefix()
    {
        var prefix = Encoding.UTF8.GetBytes(_cmdBinaryPrefix);
        if (prefix.Length == 0 || _buffer.Count >= prefix.Length)
            return false;

        for (var i = 0; i < _buffer.Count; i++)
        {
            if (!EqualsIgnoreCase(_buffer[i], prefix[i]))
                return false;
        }

        return true;
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

    private static bool EqualsIgnoreCase(byte a, byte b)
    {
        if (a == b) return true;
        if (a is >= (byte)'A' and <= (byte)'Z') return b == a + 32;
        if (b is >= (byte)'A' and <= (byte)'Z') return a == b + 32;
        return false;
    }
}
