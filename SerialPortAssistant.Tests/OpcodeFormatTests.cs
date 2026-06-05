using SerialPortAssistant.Helpers;
using Xunit;

namespace SerialPortAssistant.Tests;

public sealed class OpcodeFormatTests
{
  [Theory]
  [InlineData("0x12", 0x12)]
  [InlineData("12", 0x12)]
  [InlineData("#12", 0x12)]
  public void TryParse_AcceptsCommonForms(string input, byte expected)
  {
    Assert.True(OpcodeFormat.TryParse(input, out var opcode));
    Assert.Equal(expected, opcode);
  }

  [Fact]
  public void Format_UsesLowerHexPrefix()
  {
    Assert.Equal("0x0A", OpcodeFormat.Format(0x0A));
  }
}
