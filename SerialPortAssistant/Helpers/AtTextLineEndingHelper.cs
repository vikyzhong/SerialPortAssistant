using System.Text;
using SerialPortAssistant.Models;

namespace SerialPortAssistant.Helpers;

public static class AtTextLineEndingHelper
{
    public static string Terminator(AtTextLineEnding ending) =>
        ending == AtTextLineEnding.CrLf ? "\r\n" : "\n";

    public static string StripAnyLineEnding(string line) =>
        line.TrimEnd('\r', '\n');

    public static string EnsureTerminated(string line, AtTextLineEnding ending)
    {
        var body = StripAnyLineEnding(line);
        return string.IsNullOrEmpty(body) ? body : body + Terminator(ending);
    }

    public static byte[] ToWireBytes(string line, AtTextLineEnding ending) =>
        Encoding.UTF8.GetBytes(EnsureTerminated(line, ending));

    public static string FormatTerminatorHex(AtTextLineEnding ending) =>
        ending == AtTextLineEnding.CrLf ? "0D 0A" : "0A";
}
