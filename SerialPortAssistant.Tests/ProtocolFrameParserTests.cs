using SerialPortAssistant.Helpers;
using SerialPortAssistant.Models;
using SerialPortAssistant.Services;
using Xunit;

namespace SerialPortAssistant.Tests;

public class ProtocolFrameParserTests
{
    private static (ProtocolFrameParser Parser, LineParser LineParser) CreateParser(string dataBinaryPrefix = "DATB:")
    {
        var settings = new AppSettings
        {
            Prefixes = new FieldPrefixes { DataBinary = dataBinaryPrefix }
        };
        var lineParser = new LineParser(() => settings);
        var parser = new ProtocolFrameParser(() => settings);
        parser.Configure(lineParser);
        return (parser, lineParser);
    }

    [Fact]
    public void Append_DatbThreeChannels_ParsesBigEndianValues()
    {
        var (parser, _) = CreateParser();
        var frame = DatbFrameHelper.Build([16384, 16402, 16390]);

        var results = parser.Append(frame).ToList();

        Assert.Single(results);
        Assert.Equal(LineKind.Data, results[0].Kind);
        Assert.Equal(3, results[0].Channels.Length);
        Assert.Equal(16384, results[0].Channels[0]);
        Assert.Equal(16402, results[0].Channels[1]);
        Assert.Equal(16390, results[0].Channels[2]);
    }

    [Fact]
    public void Append_DatbSingleChannel_ParsesOneValue()
    {
        var (parser, _) = CreateParser();
        var frame = DatbFrameHelper.Build([512]);

        var results = parser.Append(frame).ToList();

        Assert.Single(results);
        Assert.Equal(512, results[0].Channels[0]);
    }

    [Fact]
    public void Append_DatbSplitAcrossChunks_ReassemblesFrame()
    {
        var (parser, _) = CreateParser();
        var frame = DatbFrameHelper.Build([1000, 2000]);

        var first = parser.Append(frame.AsSpan(0, 6)).ToList();
        Assert.Empty(first);

        var second = parser.Append(frame.AsSpan(6)).ToList();
        Assert.Single(second);
        Assert.Equal(2, second[0].Channels.Length);
        Assert.Equal(1000, second[0].Channels[0]);
        Assert.Equal(2000, second[0].Channels[1]);
    }

    [Fact]
    public void Append_DatbThenLog_ParsesBoth()
    {
        var (parser, _) = CreateParser();
        var datb = DatbFrameHelper.Build([100]);
        var log = "LOG:1,hello\n"u8.ToArray();
        var combined = datb.Concat(log).ToArray();

        var results = parser.Append(combined).ToList();

        Assert.Equal(2, results.Count);
        Assert.Equal(LineKind.Data, results[0].Kind);
        Assert.Equal(LineKind.Log, results[1].Kind);
        Assert.Equal("hello", results[1].Text);
    }

    [Fact]
    public void Append_AsciiDataLine_StillWorks()
    {
        var (parser, _) = CreateParser();
        var results = parser.Append("DATA:1000,2000,3000\n"u8).ToList();

        Assert.Single(results);
        Assert.Equal(3, results[0].Channels.Length);
        Assert.Equal(1000, results[0].Channels[0]);
    }

    [Fact]
    public void Append_DatbWithNewlineInValue_DoesNotBreakFraming()
    {
        var (parser, _) = CreateParser();
        // 0x000A would be newline in ASCII path; DATB carries it inside uint16 safely.
        var frame = DatbFrameHelper.Build([(ushort)0x000A, (ushort)0x1234]);

        var results = parser.Append(frame).ToList();

        Assert.Single(results);
        Assert.Equal(0x000A, results[0].Channels[0]);
        Assert.Equal(0x1234, results[0].Channels[1]);
    }

    [Fact]
    public void Append_AckbFrame_ParsesStatusAndOpcode()
    {
        var settings = new AppSettings();
        var lineParser = new LineParser(() => settings);
        var parser = new ProtocolFrameParser(() => settings);
        parser.Configure(lineParser);

        var frame = AckbFrameHelper.BuildOpcode(0x02, AckbFrameHelper.StatusOk);
        var results = parser.Append(frame).ToList();

        Assert.Single(results);
        Assert.Equal(LineKind.Ack, results[0].Kind);
        Assert.Equal(0x02, results[0].BinaryOpcode);
        Assert.Equal(0, results[0].BinaryStatus);
        Assert.Contains("OK", results[0].Text);
        Assert.Contains("读版本", results[0].Text);
    }
}
