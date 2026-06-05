using System.Text;

namespace SerialPortAssistant.Helpers;

public static class TrafficFormatter
{
    public static string FormatHex(byte[] data)
    {
        if (data.Length == 0) return string.Empty;
        return string.Join(' ', data.Select(b => b.ToString("X2")));
    }

    public static string FormatText(byte[] data)
    {
        var sb = new StringBuilder(data.Length);
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
