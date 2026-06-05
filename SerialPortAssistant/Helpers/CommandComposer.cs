using SerialPortAssistant.Models;

namespace SerialPortAssistant.Helpers;

public static class CommandComposer
{
  public static CommandDraft Compose(CommandPreset preset, AppSettings settings) =>
    Compose(preset.Payload, preset.HexPayload, settings);

  public static CommandDraft Compose(string textBody, string payloadHex, AppSettings settings)
  {
    var prefixes = settings.Prefixes;
    var opcodes = settings.CommandOpcodes;
    var body = StripCmdPrefix(textBody.Trim(), prefixes.Cmd);

    byte[] payload;
    if (!string.IsNullOrWhiteSpace(payloadHex))
    {
      payload = SerialMonitorHelper.ParseHexPayload(payloadHex);
    }
    else
    {
      var def = CommandOpcodeResolver.FindByCommandText(body, opcodes);
      payload = def != null ? [def.Opcode] : [];
    }

    if (payload.Length == 0)
    {
      var def = CommandOpcodeResolver.FindByCommandText(body, opcodes);
      if (def != null)
        payload = [def.Opcode];
    }

    var textBundle = CommandFrameEncoder.BuildTextSend(body, prefixes, settings.AtTextLineEnding);
    var textBytes = AtTextLineEndingHelper.ToWireBytes(textBundle.TextLine!, settings.AtTextLineEnding);

    byte[] binaryFrame;
    string binaryHex;
    if (payload.Length > 0)
    {
      var binaryBundle = CommandFrameEncoder.BuildHexSend(
        CommandOpcodeResolver.FormatPayloadHex(payload[0], payload.AsSpan(1)), prefixes);
      binaryFrame = binaryBundle.RawFrame!;
      binaryHex = binaryBundle.PreviewHex;
    }
    else
    {
      binaryFrame = [];
      binaryHex = "—";
    }

    var hint = payload.Length > 0
      ? CommandOpcodeResolver.FormatResolvedHint(payload[0], opcodes)
      : body;

    return new CommandDraft
    {
      TextBody = body,
      PayloadHex = payload.Length > 0
        ? CommandOpcodeResolver.FormatPayloadHex(payload[0], payload.AsSpan(1))
        : string.Empty,
      TextFrameDisplay = textBundle.TextLine ?? body,
      BinaryFrameHex = binaryHex,
      TextFrameBytes = textBytes,
      BinaryFrame = binaryFrame,
      ResolvedHint = hint
    };
  }

  public static string FormatHistoryKey(CommandWireFormat wire, string textBody, string payloadHex) =>
    $"{wire}|{textBody}|{payloadHex}";

  public static string FormatHistoryDisplay(CommandWireFormat wire, CommandDraft draft)
  {
    var wireLabel = wire == CommandWireFormat.Binary ? "二进制" : "文本";
    var shortHex = draft.BinaryFrame.Length > 0
      ? TrafficFormatter.FormatHex(draft.BinaryFrame.Take(Math.Min(6, draft.BinaryFrame.Length)).ToArray()) + "…"
      : draft.TextFrameDisplay;
    return $"{draft.ResolvedHint} · {wireLabel} · {shortHex}";
  }

  private static string StripCmdPrefix(string text, string prefix)
  {
    if (string.IsNullOrEmpty(prefix))
      return text;

    return text.StartsWith(prefix, StringComparison.OrdinalIgnoreCase)
      ? text[prefix.Length..].TrimStart()
      : text;
  }
}
