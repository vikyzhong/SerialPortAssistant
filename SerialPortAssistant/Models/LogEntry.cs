namespace SerialPortAssistant.Models;

public sealed class LogEntry
{
    public required string Timestamp { get; init; }
    /// <summary>LOG 等级 0~4；-1 表示 RAW/CMD 等非等级行。</summary>
    public int LevelId { get; init; } = -1;
    public required string Level { get; init; }
    public required string Message { get; init; }
    public required string DisplayText { get; init; }
    public required string Color { get; init; }
}
