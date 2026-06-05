using SerialPortAssistant.Helpers;
using SerialPortAssistant.Models;
using Xunit;

namespace SerialPortAssistant.Tests;

public class CommandFrameEncoderTests
{
  private static readonly List<CommandOpcodeDefinition> DefaultOpcodes = CommandOpcodeDefinition.CreateDefaults();

  [Fact]
  public void BuildHexSend_ProducesCmdFrame()
  {
    var prefixes = new FieldPrefixes();
    var bundle = CommandFrameEncoder.BuildHexSend("02", prefixes);

    Assert.NotNull(bundle.RawFrame);
    Assert.Equal("CMD 0x02", bundle.DisplayText);
    Assert.True(bundle.UsedBinaryWire);
    Assert.Equal((byte)'\n', bundle.RawFrame![^1]);
  }

  [Fact]
  public void BuildTextSend_CrLf_AppendsCrLfHex()
  {
    var bundle = CommandFrameEncoder.BuildTextSend("GMR", new FieldPrefixes(), AtTextLineEnding.CrLf);
    Assert.Equal("AT+GMR", bundle.TextLine);
    Assert.EndsWith("0D 0A", bundle.PreviewHex);
  }

  [Fact]
  public void BuildTextSend_Lf_AppendsLfHex()
  {
    var bundle = CommandFrameEncoder.BuildTextSend("GMR", new FieldPrefixes(), AtTextLineEnding.Lf);
    Assert.EndsWith("0A", bundle.PreviewHex);
    Assert.DoesNotContain("0D", bundle.PreviewHex);
  }

  [Fact]
  public void BuildSend_TextMode_UsesGmr()
  {
    var bundle = CommandFrameEncoder.BuildSend(
      "GMR",
      "02",
      CommandWireFormat.Text,
      new FieldPrefixes(),
      DefaultOpcodes);

    Assert.False(bundle.UsedBinaryWire);
    Assert.Equal("AT+GMR", bundle.TextLine);
  }

  [Fact]
  public void BuildSend_BinaryMode_UsesCmd()
  {
    var bundle = CommandFrameEncoder.BuildSend(
      "GMR",
      "02",
      CommandWireFormat.Binary,
      new FieldPrefixes(),
      DefaultOpcodes);

    Assert.True(bundle.UsedBinaryWire);
    Assert.NotNull(bundle.RawFrame);
  }

  [Fact]
  public void BuildSend_BinaryMode_ResolvesOpcodeFromCommandNameWhenBytesEmpty()
  {
    var bundle = CommandFrameEncoder.BuildSend(
      "GMR",
      string.Empty,
      CommandWireFormat.Binary,
      new FieldPrefixes(),
      DefaultOpcodes);

    Assert.True(bundle.UsedBinaryWire);
    Assert.Equal("CMD 0x02", bundle.DisplayText);
  }
}
