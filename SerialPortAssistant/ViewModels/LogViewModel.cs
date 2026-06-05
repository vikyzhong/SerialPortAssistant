using System.Collections.ObjectModel;
using System.IO;
using System.Text;
using System.Windows;
using Microsoft.Win32;
using SerialPortAssistant.Models;
namespace SerialPortAssistant.ViewModels;

public sealed class LogViewModel : ViewModelBase
{
    private const int MaxEntries = 5000;

    private readonly Func<AppSettings> _getSettings;
    private readonly Action<AppSettings>? _saveSettings;
    private bool _autoScroll = true;
    private bool _showRawLog;

    public ObservableCollection<LogEntry> Entries { get; } = new();
    public ObservableCollection<LogLevelFilterItem> LevelFilters { get; } = new();

    public bool AutoScroll
    {
        get => _autoScroll;
        set => SetProperty(ref _autoScroll, value);
    }

    public bool ShowRawLog
    {
        get => _showRawLog;
        set
        {
            if (!SetProperty(ref _showRawLog, value))
                return;

            PersistShowRawLog();
            ApplyRawFilterToExistingEntries();
        }
    }

    public RelayCommand ClearCommand { get; }
    public RelayCommand SaveCommand { get; }
    public RelayCommand CopyCommand { get; }
    public RelayCommand SelectAllLevelsCommand { get; }
    public RelayCommand DeselectAllLevelsCommand { get; }

    public LogViewModel(Func<AppSettings> getSettings, Action<AppSettings>? saveSettings = null)
    {
        _getSettings = getSettings;
        _saveSettings = saveSettings;

        ClearCommand = new RelayCommand(Clear);
        SaveCommand = new RelayCommand(Save);
        CopyCommand = new RelayCommand(Copy);
        SelectAllLevelsCommand = new RelayCommand(() => SetAllLevelVisibility(true));
        DeselectAllLevelsCommand = new RelayCommand(() => SetAllLevelVisibility(false));

        ReloadLevelFilters();
        _showRawLog = _getSettings().ShowRawLog;
        OnPropertyChanged(nameof(ShowRawLog));
    }

    public void ReloadLevelFilters()
    {
        foreach (var item in LevelFilters)
            item.VisibilityChanged -= OnLevelVisibilityChanged;

        LevelFilters.Clear();

        var levels = LogLevelConfig.Normalize(_getSettings().LogLevels);
        foreach (var level in levels.OrderBy(l => l.LevelId))
        {
            var item = new LogLevelFilterItem
            {
                LevelId = level.LevelId,
                Name = level.Name,
                Color = level.Color,
                IsVisible = level.IsVisible
            };
            item.VisibilityChanged += OnLevelVisibilityChanged;
            LevelFilters.Add(item);
        }
    }

    private void OnLevelVisibilityChanged(object? sender, EventArgs e)
    {
        PersistLevelVisibility();
        ApplyFilterToExistingEntries();
    }

    private void PersistLevelVisibility()
    {
        var settings = _getSettings();
        var levels = LogLevelConfig.Normalize(settings.LogLevels);
        foreach (var filter in LevelFilters)
        {
            var cfg = levels.First(l => l.LevelId == filter.LevelId);
            cfg.IsVisible = filter.IsVisible;
            cfg.Name = filter.Name;
            cfg.Color = filter.Color;
        }

        settings.LogLevels = levels;
        settings.ShowRawLog = ShowRawLog;
        _saveSettings?.Invoke(settings);
    }

    private void PersistShowRawLog()
    {
        var settings = _getSettings();
        settings.ShowRawLog = ShowRawLog;
        _saveSettings?.Invoke(settings);
    }

    private void ApplyFilterToExistingEntries()
    {
        var visibleIds = LevelFilters.Where(f => f.IsVisible).Select(f => f.LevelId).ToHashSet();
        var toRemove = Entries.Where(e => e.LevelId >= 0 && !visibleIds.Contains(e.LevelId)).ToList();
        foreach (var entry in toRemove)
            Entries.Remove(entry);

        if (!ShowRawLog)
            ApplyRawFilterToExistingEntries();
    }

    private void ApplyRawFilterToExistingEntries()
    {
        if (ShowRawLog)
            return;

        var rawEntries = Entries.Where(e => e.Level == "RAW").ToList();
        foreach (var entry in rawEntries)
            Entries.Remove(entry);
    }

    private void SetAllLevelVisibility(bool visible)
    {
        foreach (var f in LevelFilters)
            f.IsVisible = visible;
    }

    public bool IsLevelVisible(int levelId)
    {
        var filter = LevelFilters.FirstOrDefault(f => f.LevelId == levelId);
        return filter?.IsVisible ?? true;
    }

    public void HandleParsedLine(ParsedLine line)
    {
        var timestamp = DateTime.Now.ToString("HH:mm:ss.fff");

        switch (line.Kind)
        {
            case LineKind.Log:
                if (!IsLevelVisible(line.LogLevelId))
                    return;

                var levelName = line.LogLevelName ?? $"L{line.LogLevelId}";
                var color = LevelFilters.FirstOrDefault(f => f.LevelId == line.LogLevelId)?.Color ?? "#424242";
                AddLogEntry(timestamp, line.LogLevelId, levelName, line.Text, color);
                break;
            case LineKind.Raw when ShowRawLog && !string.IsNullOrWhiteSpace(line.Text):
                AddLogEntry(timestamp, -1, "RAW", line.Text, "#757575");
                break;
            case LineKind.Cmd:
                AddLogEntry(timestamp, -1, "AT-RX", line.Text, "#7B1FA2");
                break;
        }
    }

    private void AddLogEntry(string timestamp, int levelId, string level, string message, string color)
    {
        var entry = new LogEntry
        {
            Timestamp = timestamp,
            LevelId = levelId,
            Level = level,
            Message = message,
            Color = color,
            DisplayText = levelId >= 0
                ? $"[{timestamp}] [{levelId}:{level}] {message}"
                : $"[{timestamp}] [{level}] {message}"
        };

        while (Entries.Count >= MaxEntries)
            Entries.RemoveAt(0);

        Entries.Add(entry);
    }

    public void ReloadLevelStyles()
    {
        _showRawLog = _getSettings().ShowRawLog;
        OnPropertyChanged(nameof(ShowRawLog));
        ReloadLevelFilters();
    }

    private void Clear() => Entries.Clear();

    private void Save()
    {
        var dialog = new SaveFileDialog
        {
            Filter = "文本文件 (*.txt)|*.txt|所有文件 (*.*)|*.*",
            FileName = $"serial-log-{DateTime.Now:yyyyMMdd-HHmmss}.txt"
        };

        if (dialog.ShowDialog() != true)
            return;

        var sb = new StringBuilder();
        foreach (var e in Entries)
            sb.AppendLine(e.DisplayText);

        File.WriteAllText(dialog.FileName, sb.ToString(), Encoding.UTF8);
    }

    private void Copy()
    {
        var sb = new StringBuilder();
        foreach (var e in Entries)
            sb.AppendLine(e.DisplayText);

        if (sb.Length > 0)
            Clipboard.SetText(sb.ToString());
    }
}
