using System.Text;
using SerialPortAssistant.Models;

namespace SerialPortAssistant.Helpers;

public sealed class CommandSendBundle
{
  public byte[]? RawFrame { get; init; }
  public string? TextLine { get; init; }
  public required string DisplayText { get; init; }
  public required string PreviewHex { get; init; }
  public bool UsedBinaryWire { get; init; }
}

public static class CommandFrameEncoder
{
  public static CommandSendBundle BuildSend(
    string commandText,
    string hexPayload,
    CommandWireFormat wireFormat,
    FieldPrefixes prefixes,
    IReadOnlyList<CommandOpcodeDefinition> opcodes,
    AtTextLineEnding atLineEnding = AtTextLineEnding.CrLf)
  {
    if (wireFormat == CommandWireFormat.Auto)
      wireFormat = CommandWireFormat.Text;

    var textBody = StripCmdPrefix(commandText, prefixes.Cmd).Trim();
    byte[] payloadBytes;
    try
    {
      payloadBytes = SerialMonitorHelper.ParseHexPayload(hexPayload);
    }
    catch
    {
      payloadBytes = [];
    }

    if (wireFormat == CommandWireFormat.Binary)
    {
      if (payloadBytes.Length > 0)
        return BuildHexSend(hexPayload, prefixes);

      var fromText = CommandOpcodeResolver.FindByCommandText(textBody, opcodes);
      if (fromText != null)
        return BuildHexSend(fromText.Opcode.ToString("X2"), prefixes);

      throw new InvalidOperationException("字节区不能为空（二进制模式）。");
    }

    if (string.IsNullOrEmpty(textBody))
      throw new InvalidOperationException("命令名不能为空（文本模式）。");

    return BuildTextSend(textBody, prefixes, atLineEnding);
  }

  public static CommandSendBundle BuildTextSend(
    string input,
    FieldPrefixes prefixes,
    AtTextLineEnding atLineEnding = AtTextLineEnding.CrLf)
  {
    var body = input.Trim();
    if (string.IsNullOrEmpty(body))
      throw new InvalidOperationException("命令内容不能为空。");

    var prefix = prefixes.Cmd;
    var line = string.IsNullOrEmpty(prefix)
      ? body
      : body.StartsWith(prefix, StringComparison.OrdinalIgnoreCase)
        ? body
        : prefix + body.TrimStart();

    var bytes = AtTextLineEndingHelper.ToWireBytes(line, atLineEnding);
    return new CommandSendBundle
    {
      TextLine = line,
      DisplayText = line,
      PreviewHex = TrafficFormatter.FormatHex(bytes),
      UsedBinaryWire = false
    };
  }

  public static CommandSendBundle BuildHexSend(string hexInput, FieldPrefixes prefixes)
  {
    var payload = SerialMonitorHelper.ParseHexPayload(hexInput);
    if (payload.Length == 0)
      throw new InvalidOperationException("字节区不能为空。");

    var frame = CmdbFrameHelper.Build(payload, prefixes.CmdBinary);
    return new CommandSendBundle
    {
      RawFrame = frame,
      DisplayText = DescribeCmdbPayload(payload),
      PreviewHex = TrafficFormatter.FormatHex(frame),
      UsedBinaryWire = true
    };
  }

  public static string FormatPreviewLine(CommandSendBundle bundle, string? resolvedHint)
  {
    var hint = string.IsNullOrEmpty(resolvedHint) ? string.Empty : $" ({resolvedHint})";
    if (bundle.UsedBinaryWire)
      return $"将发送 CMD{hint}：{bundle.PreviewHex}";

    return $"将发送 AT 文本{hint}：{bundle.PreviewHex}";
  }

  public static string FormatAckBinaryDisplay(
    byte opcode,
    byte status,
    ReadOnlySpan<byte> extra,
    IReadOnlyList<CommandOpcodeDefinition> opcodes)
  {
    var name = ResolveOpcodeName(opcode, opcodes);
    var statusText = status == AckbFrameHelper.StatusOk ? "OK" : status == AckbFrameHelper.StatusErr ? "ERR" : $"0x{status:X2}";
    var extraText = extra.Length > 0 ? $"  data={TrafficFormatter.FormatHex(extra.ToArray())}" : string.Empty;
    return $"{statusText} {OpcodeFormat.Format(opcode)} {name}{extraText}";
  }

  public static string DescribeCmdbPayload(ReadOnlySpan<byte> payload)
  {
    if (payload.Length == 0)
      return "CMD (empty)";

    var args = payload.Length > 1
      ? TrafficFormatter.FormatHex(payload[1..].ToArray())
      : string.Empty;
    var argsPart = args.Length > 0 ? $" [{args}]" : string.Empty;
    return $"CMD {OpcodeFormat.Format(payload[0])}{argsPart}";
  }

  public static string ResolveOpcodeName(byte opcode, IReadOnlyList<CommandOpcodeDefinition> opcodes)
  {
    var match = opcodes.FirstOrDefault(o => o.Opcode == opcode);
    return match?.Name ?? $"CMD 0x{opcode:X2}";
  }

  public static byte? ResolveOpcodeFromTextAlias(string textBody, IReadOnlyList<CommandOpcodeDefinition> opcodes) =>
    CommandOpcodeResolver.FindByCommandText(textBody, opcodes)?.Opcode;

  private static string StripCmdPrefix(string text, string prefix)
  {
    if (string.IsNullOrEmpty(prefix))
      return text;

    return text.StartsWith(prefix, StringComparison.OrdinalIgnoreCase)
      ? text[prefix.Length..].TrimStart()
      : text;
  }
}
