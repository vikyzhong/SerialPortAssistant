namespace SerialPortAssistant.Models;

public sealed class AckEntry
{
    public required string Timestamp { get; init; }
    public required string Payload { get; init; }
    public required string DisplayText { get; init; }
    public bool IsLatest { get; set; }
}
