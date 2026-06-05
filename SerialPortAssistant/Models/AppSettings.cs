namespace SerialPortAssistant.Models;

public sealed class AppSettings
{
    public FieldPrefixes Prefixes { get; set; } = new();
    public List<LogLevelConfig> LogLevels { get; set; } = LogLevelConfig.CreateDefaults();
    public List<CommandPreset> Commands { get; set; } = [];
    public List<CommandOpcodeDefinition> CommandOpcodes { get; set; } = CommandOpcodeDefinition.CreateDefaults();
    /// <summary>快捷栏引用的 Opcode 子集。</summary>
    public List<byte> FavoriteOpcodes { get; set; } = [0x01, 0x02, 0x0A];
    public CommandWireFormat LastCommandWireFormat { get; set; } = CommandWireFormat.Text;
    public List<SentCommandHistoryItem> SendHistory { get; set; } = [];
    public int MaxCommandHistory { get; set; } = 30;
    public int MaxAckEntries { get; set; } = 100;
    public DataChannelNames DataChannels { get; set; } = DataChannelNames.CreateAccelerometerDefaults();
    /// <summary>日志区是否显示未识别协议行 (RAW)。</summary>
    public bool ShowRawLog { get; set; }
    public SerialPortPreferences SerialPort { get; set; } = new();
    /// <summary>AT+ 文本命令行尾（仅影响 AT+ 发送；DATA/LOG/CMD/ACK 仍为 LF）。</summary>
    public AtTextLineEnding AtTextLineEnding { get; set; } = AtTextLineEnding.CrLf;
    /// <summary>命令码表迁移版本；&lt;2 时重置为 AT+ 标准命令集。</summary>
    public int AtCommandSetVersion { get; set; }
}
