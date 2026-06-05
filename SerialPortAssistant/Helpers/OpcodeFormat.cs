using System.Globalization;

namespace SerialPortAssistant.Helpers;

public static class OpcodeFormat
{
  public static string Format(byte opcode) =>
    $"0x{opcode:X2}";

  public static bool TryParse(string? input, out byte opcode)
  {
    opcode = 0;
    if (string.IsNullOrWhiteSpace(input))
      return false;

    var token = input.Trim();
    if (token.StartsWith('#'))
      token = token[1..].Trim();

    if (token.StartsWith("0x", StringComparison.OrdinalIgnoreCase))
      token = token[2..].Trim();

    return byte.TryParse(token, NumberStyles.HexNumber, CultureInfo.InvariantCulture, out opcode);
  }
}
