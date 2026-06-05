using SerialPortAssistant.Models;

namespace SerialPortAssistant.Helpers;

public static class CommandOpcodeCatalog
{
  public static void ApplyMigration(AppSettings settings)
  {
    foreach (var cmd in settings.Commands)
      cmd.ApplyLegacyMigration();

    MigrateAtCommandSet(settings);

    if (settings.FavoriteOpcodes.Count == 0 && settings.Commands.Count > 0)
    {
      foreach (var preset in settings.Commands)
      {
        var match = settings.CommandOpcodes.FirstOrDefault(o =>
          string.Equals(o.Name, preset.Name, StringComparison.OrdinalIgnoreCase) ||
          (!string.IsNullOrEmpty(o.TextAlias) &&
           string.Equals(o.TextAlias, preset.Payload, StringComparison.OrdinalIgnoreCase)));

        if (match != null && !settings.FavoriteOpcodes.Contains(match.Opcode))
          settings.FavoriteOpcodes.Add(match.Opcode);
      }
    }

    if (settings.FavoriteOpcodes.Count == 0)
    {
      foreach (var op in settings.CommandOpcodes.Take(3))
        settings.FavoriteOpcodes.Add(op.Opcode);
    }

    if (settings.LastCommandWireFormat == CommandWireFormat.Auto)
      settings.LastCommandWireFormat = CommandWireFormat.Text;

    foreach (var op in settings.CommandOpcodes)
    {
      if (!string.IsNullOrWhiteSpace(op.DefaultPayloadHex))
        op.DefaultPayloadHex = NormalizePayloadDisplay(op.DefaultPayloadHex);
    }

    var cmdPrefix = settings.Prefixes.Cmd ?? string.Empty;
    if (string.IsNullOrWhiteSpace(cmdPrefix) ||
        cmdPrefix.StartsWith("CMD", StringComparison.OrdinalIgnoreCase))
      settings.Prefixes.Cmd = "AT+";

    var cmdBin = settings.Prefixes.CmdBinary ?? string.Empty;
    if (string.IsNullOrWhiteSpace(cmdBin) ||
        cmdBin.StartsWith("CMDB", StringComparison.OrdinalIgnoreCase))
      settings.Prefixes.CmdBinary = "CMD";

    var ackBin = settings.Prefixes.AckBinary ?? string.Empty;
    if (string.IsNullOrWhiteSpace(ackBin) ||
        ackBin.StartsWith("ACKB", StringComparison.OrdinalIgnoreCase))
      settings.Prefixes.AckBinary = "ACK";
  }

  private const int CurrentAtCommandSetVersion = 3;

  /// <summary>迁移 AT+ 命令集：v0/v1 全量重置；v2 补全新增命令。</summary>
  private static void MigrateAtCommandSet(AppSettings settings)
  {
    if (settings.AtCommandSetVersion >= CurrentAtCommandSetVersion)
      return;

    if (settings.AtCommandSetVersion < 2)
    {
      settings.CommandOpcodes = CommandOpcodeDefinition.CreateDefaults();
      settings.FavoriteOpcodes = [0x01, 0x02, 0x0A];
      settings.Commands = [];
    }
    else
    {
      MergeAtCommandDefaults(settings);
      if (settings.FavoriteOpcodes.Count <= 1)
        settings.FavoriteOpcodes = [0x01, 0x02, 0x0A];
    }

    settings.AtCommandSetVersion = CurrentAtCommandSetVersion;
  }

  private static void MergeAtCommandDefaults(AppSettings settings)
  {
    foreach (var d in CommandOpcodeDefinition.CreateDefaults())
    {
      if (settings.CommandOpcodes.Any(o => o.Opcode == d.Opcode))
        continue;

      settings.CommandOpcodes.Add(new CommandOpcodeDefinition
      {
        Opcode = d.Opcode,
        Name = d.Name,
        TextAlias = d.TextAlias,
        DefaultPayloadHex = d.DefaultPayloadHex
      });
    }

    settings.CommandOpcodes = settings.CommandOpcodes.OrderBy(o => o.Opcode).ToList();
  }

  public static IEnumerable<CommandOpcodeDefinition> ResolveFavorites(AppSettings settings)
  {
    foreach (var opcode in settings.FavoriteOpcodes)
    {
      var def = settings.CommandOpcodes.FirstOrDefault(o => o.Opcode == opcode);
      if (def != null)
        yield return def;
    }
  }

  public static string DefaultPayloadHex(CommandOpcodeDefinition definition)
  {
    if (!string.IsNullOrWhiteSpace(definition.DefaultPayloadHex))
      return NormalizePayloadDisplay(definition.DefaultPayloadHex.Trim());

    return HexPayloadFormat.FormatPayload(definition.Opcode, default);
  }

  /// <summary>将 payload 规范为空格分隔 HEX（无 0x 前缀）。</summary>
  public static string NormalizePayloadDisplay(string payloadHex)
  {
    if (string.IsNullOrWhiteSpace(payloadHex))
      return string.Empty;

    try
    {
      var bytes = SerialMonitorHelper.ParseHexPayload(payloadHex);
      if (bytes.Length == 0)
        return payloadHex;

      var args = bytes.Length > 1 ? bytes.AsSpan(1) : ReadOnlySpan<byte>.Empty;
      return CommandOpcodeResolver.FormatPayloadHex(bytes[0], args);
    }
    catch
    {
      return payloadHex;
    }
  }

  /// <summary>以对话框 Opcode 为准，替换 payload 首字节并保留后续参数。</summary>
  public static bool TryAlignPayloadToOpcode(byte opcode, string payloadHex, out string alignedHex, out string? error)
  {
    alignedHex = string.Empty;
    error = null;
    try
    {
      var bytes = string.IsNullOrWhiteSpace(payloadHex)
        ? Array.Empty<byte>()
        : SerialMonitorHelper.ParseHexPayload(payloadHex);

      var args = bytes.Length > 1 ? bytes.AsSpan(1) : ReadOnlySpan<byte>.Empty;
      alignedHex = CommandOpcodeResolver.FormatPayloadHex(opcode, args);
      return true;
    }
    catch (Exception ex)
    {
      error = ex.Message;
      return false;
    }
  }

  public static bool TryParsePayloadHex(string? hex, out byte opcode, out string? error)
  {
    opcode = 0;
    error = null;
    if (string.IsNullOrWhiteSpace(hex))
    {
      error = "二进制 payload 不能为空。";
      return false;
    }

    try
    {
      var bytes = SerialMonitorHelper.ParseHexPayload(hex);
      if (bytes.Length == 0)
      {
        error = "二进制 payload 不能为空。";
        return false;
      }

      opcode = bytes[0];
      return true;
    }
    catch (Exception ex)
    {
      error = ex.Message;
      return false;
    }
  }

  public sealed class SaveValidationResult
  {
    public bool Success { get; init; }
    public string? ErrorMessage { get; init; }
    public CommandOpcodeDefinition? Definition { get; init; }
  }

  public static SaveValidationResult ValidateNewCommand(
    byte opcode,
    string name,
    string? textAlias,
    string payloadHex,
    IReadOnlyList<CommandOpcodeDefinition> existing)
  {
    name = name.Trim();
    textAlias = string.IsNullOrWhiteSpace(textAlias) ? null : textAlias.Trim();

    if (string.IsNullOrEmpty(name))
      return Fail("显示名称不能为空。");

    if (!TryAlignPayloadToOpcode(opcode, payloadHex, out var alignedPayloadHex, out var alignError))
      return Fail(alignError!);

    payloadHex = alignedPayloadHex;

    if (textAlias == null)
      return Fail("文本别名不能为空。");

    if (existing.Any(o => o.Opcode == opcode))
      return Fail($"Opcode 0x{opcode:X2} 已存在。");

    if (existing.Any(o => string.Equals(o.Name, name, StringComparison.OrdinalIgnoreCase)))
      return Fail($"名称「{name}」已存在。");

    if (existing.Any(o =>
          !string.IsNullOrEmpty(o.TextAlias) &&
          string.Equals(o.TextAlias, textAlias, StringComparison.OrdinalIgnoreCase)))
      return Fail($"文本别名「{textAlias}」已存在。");

    return new SaveValidationResult
    {
      Success = true,
      Definition = new CommandOpcodeDefinition
      {
        Opcode = opcode,
        Name = name,
        TextAlias = textAlias,
        DefaultPayloadHex = payloadHex.Trim()
      }
    };
  }

  private static SaveValidationResult Fail(string message) =>
    new() { Success = false, ErrorMessage = message };
}
