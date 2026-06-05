using SerialPortSimulator.Helpers;
using SerialPortSimulator.Models;

namespace SerialPortSimulator.Services;

public sealed class CmdResponse
{
    public byte[]? BinaryFrame { get; init; }
    public string? TextLine { get; init; }
    public IReadOnlyList<string>? TextLines { get; init; }
}

public sealed class CmdResponder
{
    private readonly List<CmdRule> _rules;
    private readonly string _cmdTextPrefix;

    public CmdResponder(IEnumerable<CmdRule> rules, string cmdTextPrefix = "AT+")
    {
        _rules = rules.ToList();
        _cmdTextPrefix = string.IsNullOrEmpty(cmdTextPrefix) ? "AT+" : cmdTextPrefix;
    }

    public CmdResponse? TryRespond(HostCommand command)
    {
        if (command.IsBinary)
            return TryRespondBinary(command.Opcode, command.Args);
        return TryRespondText(command.TextLine);
    }

    public CmdResponse? TryRespondText(string line)
    {
        if (!line.StartsWith(_cmdTextPrefix, StringComparison.OrdinalIgnoreCase))
            return null;

        var cmd = line[_cmdTextPrefix.Length..].Trim();
        if (cmd.Length == 0)
            return Text(AtTextResponseHelper.BuildLine("ERROR"));

        foreach (var rule in _rules.Where(r => r.Enabled))
        {
            if (rule.Opcode.HasValue && byte.TryParse(cmd, System.Globalization.NumberStyles.HexNumber, null, out var op) &&
                op == rule.Opcode.Value)
                return RespondRule(rule, op, replyBinary: false);

            if (!string.IsNullOrEmpty(rule.CommandKeyword) &&
                MatchesAtCommand(cmd, rule.CommandKeyword))
                return RespondRule(rule, rule.Opcode ?? 0, replyBinary: false);
        }

        return Text(AtTextResponseHelper.BuildLine("ERROR"));
    }

    public CmdResponse? TryRespondBinary(byte opcode, ReadOnlySpan<byte> args)
    {
        foreach (var rule in _rules.Where(r => r.Enabled))
        {
            if (rule.Opcode == opcode)
                return RespondRule(rule, opcode, replyBinary: true);
        }

        return Binary(opcode, AckbFrameHelper.StatusOk);
    }

    private CmdResponse RespondRule(CmdRule rule, byte opcode, bool replyBinary)
    {
        if (replyBinary)
            return Binary(opcode, rule.AckStatus);

        if (rule.UrcLines is { Count: > 0 })
        {
            var lines = rule.UrcLines.Select(AtTextResponseHelper.BuildLine).ToList();
            lines.Add(AtTextResponseHelper.BuildLine(rule.AckResponse));
            return new CmdResponse { TextLines = lines };
        }

        if (!string.IsNullOrWhiteSpace(rule.UrcLine))
        {
            return new CmdResponse
            {
                TextLines =
                [
                    AtTextResponseHelper.BuildLine(rule.UrcLine!),
                    AtTextResponseHelper.BuildLine(rule.AckResponse)
                ]
            };
        }

        return Text(AtTextResponseHelper.BuildLine(rule.AckResponse));
    }

    private static bool MatchesAtCommand(string cmdBody, string keyword) =>
        cmdBody.Equals(keyword, StringComparison.OrdinalIgnoreCase) ||
        cmdBody.StartsWith(keyword + "=", StringComparison.OrdinalIgnoreCase) ||
        cmdBody.StartsWith(keyword + "?", StringComparison.OrdinalIgnoreCase);

    private static CmdResponse Text(string line) => new() { TextLine = line };

    private static CmdResponse Binary(byte opcode, byte status) =>
        new() { BinaryFrame = AckbFrameHelper.Build(opcode, status) };

    /// <summary>默认 AT+ 规则（与助手命令码表一致）。</summary>
    public static List<CmdRule> CreateDefaultRules() =>
    [
        new()
        {
            Opcode = 0x01,
            CommandKeyword = "RST",
            AckResponse = "OK",
            UseBinaryAck = true,
            AckStatus = AckbFrameHelper.StatusOk
        },
        new()
        {
            Opcode = 0x02,
            CommandKeyword = "GMR",
            UrcLines =
            [
                "AT version:1.0.0(SerialPortSimulator)",
                "SDK version:v1.0.0-sim",
                "compile time:Jun  2 2026 12:00:00"
            ],
            AckResponse = "OK",
            UseBinaryAck = true,
            AckStatus = AckbFrameHelper.StatusOk
        },
        new()
        {
            Opcode = 0x03,
            CommandKeyword = "RESTORE",
            AckResponse = "OK",
            UseBinaryAck = true,
            AckStatus = AckbFrameHelper.StatusOk
        },
        new()
        {
            Opcode = 0x04,
            CommandKeyword = "GSLP",
            AckResponse = "OK",
            UseBinaryAck = true,
            AckStatus = AckbFrameHelper.StatusOk
        },
        new()
        {
            Opcode = 0x05,
            CommandKeyword = "ECHO",
            UrcLine = "+ECHO:1",
            AckResponse = "OK",
            UseBinaryAck = true,
            AckStatus = AckbFrameHelper.StatusOk
        },
        new()
        {
            Opcode = 0x06,
            CommandKeyword = "UART_CUR?",
            UrcLine = "+UART_CUR:115200,8,1,0,0",
            AckResponse = "OK",
            UseBinaryAck = true,
            AckStatus = AckbFrameHelper.StatusOk
        },
        new()
        {
            Opcode = 0x07,
            CommandKeyword = "CWMODE?",
            UrcLine = "+CWMODE:1",
            AckResponse = "OK",
            UseBinaryAck = true,
            AckStatus = AckbFrameHelper.StatusOk
        },
        new()
        {
            Opcode = 0x08,
            CommandKeyword = "CWLAP",
            UrcLines =
            [
                "+CWLAP:(3,\"TestAP\",-45,\"aa:bb:cc:dd:ee:01\",1)",
                "+CWLAP:(4,\"LabWiFi\",-62,\"aa:bb:cc:dd:ee:02\",6)"
            ],
            AckResponse = "OK",
            UseBinaryAck = true,
            AckStatus = AckbFrameHelper.StatusOk
        },
        new()
        {
            Opcode = 0x09,
            CommandKeyword = "CWJAP?",
            UrcLine = "+CWJAP:\"TestAP\",\"aa:bb:cc:dd:ee:01\",-45,1",
            AckResponse = "OK",
            UseBinaryAck = true,
            AckStatus = AckbFrameHelper.StatusOk
        },
        new()
        {
            Opcode = 0x0A,
            CommandKeyword = "PING",
            AckResponse = "OK",
            UseBinaryAck = true,
            AckStatus = AckbFrameHelper.StatusOk
        },
        new()
        {
            Opcode = 0x0B,
            CommandKeyword = "CIPSTATUS",
            UrcLine = "STATUS:2",
            AckResponse = "OK",
            UseBinaryAck = true,
            AckStatus = AckbFrameHelper.StatusOk
        },
        new()
        {
            Opcode = 0x0C,
            CommandKeyword = "SAVE",
            AckResponse = "OK",
            UseBinaryAck = true,
            AckStatus = AckbFrameHelper.StatusOk
        },
        new()
        {
            Opcode = 0x0D,
            CommandKeyword = "ATE0",
            AckResponse = "OK",
            UseBinaryAck = true,
            AckStatus = AckbFrameHelper.StatusOk
        },
        new()
        {
            Opcode = 0x0E,
            CommandKeyword = "ATE1",
            AckResponse = "OK",
            UseBinaryAck = true,
            AckStatus = AckbFrameHelper.StatusOk
        }
    ];
}
