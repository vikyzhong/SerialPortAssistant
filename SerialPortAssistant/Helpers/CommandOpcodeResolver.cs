using SerialPortAssistant.Models;

namespace SerialPortAssistant.Helpers;

public static class CommandOpcodeResolver
{
  public static CommandOpcodeDefinition? FindByOpcode(byte opcode, IReadOnlyList<CommandOpcodeDefinition> opcodes) =>
    opcodes.FirstOrDefault(o => o.Opcode == opcode);

  public static CommandOpcodeDefinition? FindByCommandText(string text, IReadOnlyList<CommandOpcodeDefinition> opcodes)
  {
    var key = text.Trim();
    if (key.Length == 0)
      return null;

    var byAlias = opcodes.FirstOrDefault(o =>
      !string.IsNullOrEmpty(o.TextAlias) &&
      string.Equals(o.TextAlias, key, StringComparison.OrdinalIgnoreCase));
    if (byAlias != null)
      return byAlias;

    return opcodes.FirstOrDefault(o =>
      string.Equals(o.Name, key, StringComparison.OrdinalIgnoreCase));
  }

  public static string DisplayCommandText(CommandOpcodeDefinition definition) =>
    !string.IsNullOrEmpty(definition.TextAlias) ? definition.TextAlias! : definition.Name;

  public static string FormatResolvedHint(byte opcode, IReadOnlyList<CommandOpcodeDefinition> opcodes)
  {
    var name = CommandFrameEncoder.ResolveOpcodeName(opcode, opcodes);
    return $"{OpcodeFormat.Format(opcode)} {name}";
  }

  public static string FormatPayloadHex(byte opcode, ReadOnlySpan<byte> args) =>
    HexPayloadFormat.FormatPayload(opcode, args);
}
