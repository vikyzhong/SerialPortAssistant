namespace SerialPortAssistant.Models;

public static class LogLevels
{
    public const int Count = 5;
    public const int MaxLevelId = Count - 1;
}

public sealed class LogLevelConfig
{
    /// <summary>等级字节 0~4，与 LOG: 后第一字节对应。</summary>
    public int LevelId { get; set; }
    public string Name { get; set; } = "INFO";
    public string Color { get; set; } = "#2196F3";
    /// <summary>是否在日志窗口显示该等级。</summary>
    public bool IsVisible { get; set; } = true;

    public static List<LogLevelConfig> CreateDefaults() =>
    [
        new() { LevelId = 0, Name = "DEBUG", Color = "#9E9E9E", IsVisible = true },
        new() { LevelId = 1, Name = "INFO", Color = "#2196F3", IsVisible = true },
        new() { LevelId = 2, Name = "WARN", Color = "#FF9800", IsVisible = true },
        new() { LevelId = 3, Name = "ERROR", Color = "#F44336", IsVisible = true },
        new() { LevelId = 4, Name = "TRACE", Color = "#9C27B0", IsVisible = false }
    ];

    public static List<LogLevelConfig> Normalize(List<LogLevelConfig>? levels)
    {
        var defaults = CreateDefaults();
        if (levels == null || levels.Count == 0)
            return defaults;

        var result = new List<LogLevelConfig>();
        for (var id = 0; id < LogLevels.Count; id++)
        {
            var existing = levels.FirstOrDefault(l => l.LevelId == id);
            if (existing != null)
            {
                existing.LevelId = id;
                result.Add(existing);
            }
            else
                result.Add(defaults[id]);
        }

        return result;
    }
}
