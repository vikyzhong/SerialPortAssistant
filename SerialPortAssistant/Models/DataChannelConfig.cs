namespace SerialPortAssistant.Models;

public static class DataChannels
{
    public const int MaxCount = 3;
}

public sealed class DataChannelNames
{
    public string Channel1 { get; set; } = "X";
    public string Channel2 { get; set; } = "Y";
    public string Channel3 { get; set; } = "Z";

    public string[] ToArray() => [Channel1, Channel2, Channel3];

    public static DataChannelNames CreateAccelerometerDefaults() => new();
}
