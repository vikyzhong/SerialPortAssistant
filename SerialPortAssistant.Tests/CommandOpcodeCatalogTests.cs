using Xunit;
using SerialPortAssistant.Helpers;
using SerialPortAssistant.Models;

namespace SerialPortAssistant.Tests;

public sealed class CommandOpcodeCatalogTests
{
  [Fact]
  public void ValidateNewCommand_RejectsDuplicateOpcode()
  {
    var existing = new List<CommandOpcodeDefinition>
    {
      new() { Opcode = 0x02, Name = "读版本", TextAlias = "GMR" }
    };

    var result = CommandOpcodeCatalog.ValidateNewCommand(
      0x02, "其他", "OTHER", "02", existing);

    Assert.False(result.Success);
    Assert.Contains("0x02", result.ErrorMessage);
  }

  [Fact]
  public void ValidateNewCommand_AllowsDistinctOpcodeNameAndAlias()
  {
    var result = CommandOpcodeCatalog.ValidateNewCommand(
      0x20, "测试命令", "TEST_NEW", "20", []);

    Assert.True(result.Success);
    Assert.Equal("TEST_NEW", result.Definition!.TextAlias);
    Assert.Equal("测试命令", result.Definition.Name);
  }

  [Fact]
  public void ValidateNewCommand_AllowsNewAliasWhileTextFrameStillShowsOldBody()
  {
    var existing = new List<CommandOpcodeDefinition>
    {
      new() { Opcode = 0x02, Name = "读版本", TextAlias = "GMR" }
    };

    var result = CommandOpcodeCatalog.ValidateNewCommand(
      0x20, "测试", "TEST_NEW", "20", existing);

    Assert.True(result.Success);
    Assert.Equal("TEST_NEW", result.Definition!.TextAlias);
  }

  [Fact]
  public void ValidateNewCommand_AlignsPayloadFirstByteToDialogOpcode()
  {
    var result = CommandOpcodeCatalog.ValidateNewCommand(
      0x12, "新命令", "NEW_CMD", "11 0A", []);

    Assert.True(result.Success);
    Assert.Equal("0x12 0A", result.Definition!.DefaultPayloadHex);
  }

  [Fact]
  public void ValidateNewCommand_AcceptsConsistentEntry()
  {
    var result = CommandOpcodeCatalog.ValidateNewCommand(
      0x20, "测试", "TEST_CMD", "20", []);

    Assert.True(result.Success);
    Assert.NotNull(result.Definition);
    Assert.Equal(0x20, result.Definition!.Opcode);
  }
}
