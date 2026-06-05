using SerialPortAssistant.Models;
using SerialPortAssistant.Services;
using Xunit;

namespace SerialPortAssistant.Tests;

public class LineParserTests
{
    private static LineParser CreateParser() =>
        new(() => new AppSettings());

    [Fact]
    public void Parse_DataThreeChannels_ReturnsUInt16()
    {
        var result = CreateParser().Parse("DATA:1000,2000,3000");
        Assert.Equal(LineKind.Data, result.Kind);
        Assert.Equal(3, result.Channels.Length);
        Assert.Equal(1000, result.Channels[0]);
    }

    [Fact]
    public void Parse_LogLevelByte_ParsesMessage()
    {
        var payload = $"{(char)2}sensor overheating";
        var result = CreateParser().Parse("LOG:" + payload);
        Assert.Equal(LineKind.Log, result.Kind);
        Assert.Equal(2, result.LogLevelId);
        Assert.Equal("WARN", result.LogLevelName);
        Assert.Equal("sensor overheating", result.Text);
    }

    [Fact]
    public void Parse_LogAsciiDigit_ParsesLevel()
    {
        var result = CreateParser().Parse("LOG:1,system ready");
        Assert.Equal(LineKind.Log, result.Kind);
        Assert.Equal(1, result.LogLevelId);
        Assert.Equal("INFO", result.LogLevelName);
        Assert.Equal("system ready", result.Text);
    }

    [Fact]
    public void Parse_LogInvalidLevel_ReturnsRaw()
    {
        var result = CreateParser().Parse("LOG:9,bad");
        Assert.Equal(LineKind.Raw, result.Kind);
    }

    [Fact]
    public void Parse_OkLine_ReturnsAck()
    {
        var result = CreateParser().Parse("OK");
        Assert.Equal(LineKind.Ack, result.Kind);
        Assert.Equal("OK", result.Text);
    }

    [Fact]
    public void Parse_ErrorLine_ReturnsAck()
    {
        var result = CreateParser().Parse("ERROR");
        Assert.Equal(LineKind.Ack, result.Kind);
        Assert.Equal("ERROR", result.Text);
    }

    [Fact]
    public void Parse_VersionLine_ReturnsAck()
    {
        var result = CreateParser().Parse("SerialPortSimulator v1.0.0-sim");
        Assert.Equal(LineKind.Raw, result.Kind);
    }

    [Fact]
    public void Parse_AtGmrCommand_ReturnsCmd()
    {
        var result = CreateParser().Parse("AT+GMR");
        Assert.Equal(LineKind.Cmd, result.Kind);
        Assert.Equal("GMR", result.Text);
    }

    [Fact]
    public void Parse_LegacyCmdsPrefix_ReturnsRaw()
    {
        var result = CreateParser().Parse("CMDS:GET_VERSION");
        Assert.Equal(LineKind.Raw, result.Kind);
    }

    [Fact]
    public void Parse_LegacyAcksPrefix_ReturnsRaw()
    {
        var result = CreateParser().Parse("ACKS:OK GET_VERSION");
        Assert.Equal(LineKind.Raw, result.Kind);
    }
}
