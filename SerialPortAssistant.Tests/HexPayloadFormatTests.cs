using SerialPortAssistant.Helpers;
using Xunit;

namespace SerialPortAssistant.Tests;

public sealed class HexPayloadFormatTests
{
    [Fact]
    public void FormatPayload_UsesSpacedBytesWithoutPrefix()
    {
        Assert.Equal("02", HexPayloadFormat.FormatPayload(0x02, ReadOnlySpan<byte>.Empty));
        Assert.Equal("04 C2 00", HexPayloadFormat.FormatPayload(0x04, new byte[] { 0xC2, 0x00 }));
    }

    [Theory]
    [InlineData("0x02", "02")]
    [InlineData("02 0a", "02 0A")]
    [InlineData("02,0A", "02 0A")]
    public void TryNormalizeDisplay_StripsPrefixAndSpacesBytes(string input, string expected)
    {
        Assert.True(HexPayloadFormat.TryNormalizeDisplay(input, out var display));
        Assert.Equal(expected, display);
    }

    [Theory]
    [InlineData("020A", "02 0A")]
    [InlineData("0x020x0A", "02 0A")]
    [InlineData("02,0A", "02 0A")]
    [InlineData("020", "02 0")]
    [InlineData("02", "02")]
    public void FormatInputLive_SplitsHexBytes(string input, string expected)
    {
        Assert.Equal(expected, HexPayloadFormat.FormatInputLive(input));
    }

    [Theory]
    [InlineData("1", true)]
    [InlineData("12", false)]
    [InlineData("12 7", true)]
    [InlineData("12 7A", false)]
    [InlineData("", false)]
    public void HasIncompleteTrailingNibble_DetectsOddDigitCount(string input, bool incomplete)
    {
        Assert.Equal(incomplete, HexPayloadFormat.HasIncompleteTrailingNibble(input));
    }
}
