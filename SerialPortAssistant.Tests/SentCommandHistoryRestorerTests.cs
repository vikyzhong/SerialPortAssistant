using SerialPortAssistant.Helpers;
using SerialPortAssistant.Models;
using Xunit;

namespace SerialPortAssistant.Tests;

public sealed class SentCommandHistoryRestorerTests
{
    private static readonly FieldPrefixes Prefixes = new();

    [Fact]
    public void TryRestore_AtGmrLine_ParsesBody()
    {
        var item = new SentCommandHistoryItem { TextLine = "AT+GMR" };
        var kind = SentCommandHistoryRestorer.TryRestore(item, Prefixes, out var body, out _, out _);
        Assert.Equal(SentCommandRestoreKind.Cmds, kind);
        Assert.Equal("GMR", body);
    }

    [Fact]
    public void TryRestore_CmdbFrame_ParsesPayloadHex()
    {
        var frame = CmdbFrameHelper.BuildOpcode(0x02);
        var item = new SentCommandHistoryItem
        {
            RawFrameHex = TrafficFormatter.FormatHex(frame),
            UsedBinaryWire = true
        };
        var kind = SentCommandHistoryRestorer.TryRestore(item, Prefixes, out _, out var payloadHex, out _);
        Assert.Equal(SentCommandRestoreKind.Cmdb, kind);
        Assert.Equal("02", payloadHex);
    }

    [Fact]
    public void TryRestore_UnknownText_FallsBackToRawHex()
    {
        var item = new SentCommandHistoryItem { TextLine = "HELLO" };
        var kind = SentCommandHistoryRestorer.TryRestore(item, Prefixes, out _, out _, out var rawHex);
        Assert.Equal(SentCommandRestoreKind.Raw, kind);
        Assert.Contains("48", rawHex);
    }
}
