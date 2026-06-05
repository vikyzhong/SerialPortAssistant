using System.Collections.ObjectModel;
using System.IO;
using System.Windows;
using Microsoft.Win32;
using SerialPortAssistant.Helpers;
using SerialPortAssistant.Models;

namespace SerialPortAssistant.ViewModels;

public sealed class TrafficMonitorViewModel : ViewModelBase
{
    private const int MaxEntries = 3000;

    private readonly List<TrafficRecord> _records = new();
    private readonly List<byte> _rxPending = new();
    private readonly List<byte> _txPending = new();
    private bool _isHexDisplay;
    private bool _autoScroll = true;
    private bool _isPaused;

    public ObservableCollection<string> DisplayLines { get; } = new();

    public bool IsHexDisplay
    {
        get => _isHexDisplay;
        set
        {
            if (SetProperty(ref _isHexDisplay, value))
                RebuildDisplay();
        }
    }

    public bool AutoScroll
    {
        get => _autoScroll;
        set => SetProperty(ref _autoScroll, value);
    }

    public bool IsPaused
    {
        get => _isPaused;
        set
        {
            if (SetProperty(ref _isPaused, value))
                OnPropertyChanged(nameof(PauseButtonText));
        }
    }

    public string PauseButtonText => IsPaused ? "继续" : "暂停";

    public string DisplayModeLabel => IsHexDisplay ? "HEX" : "字符串";

    public RelayCommand ToggleDisplayModeCommand { get; }
    public RelayCommand ClearCommand { get; }
    public RelayCommand SaveCommand { get; }
    public RelayCommand TogglePauseCommand { get; }

    public TrafficMonitorViewModel()
    {
        ToggleDisplayModeCommand = new RelayCommand(() => IsHexDisplay = !IsHexDisplay);
        ClearCommand = new RelayCommand(Clear);
        SaveCommand = new RelayCommand(Save);
        TogglePauseCommand = new RelayCommand(() => IsPaused = !IsPaused);
    }

    public void Add(TrafficDirection direction, byte[] data)
    {
        if (IsPaused || data.Length == 0)
            return;

        Application.Current.Dispatcher.BeginInvoke(() =>
        {
            var pending = direction == TrafficDirection.Rx ? _rxPending : _txPending;
            pending.AddRange(data);
            DrainPending(pending, direction);
        });
    }

    public void FlushPending()
    {
        if (Application.Current.Dispatcher.CheckAccess())
        {
            FlushPendingCore();
            return;
        }

        Application.Current.Dispatcher.Invoke(FlushPendingCore);
    }

    private void FlushPendingCore()
    {
        FlushBuffer(_rxPending, TrafficDirection.Rx);
        FlushBuffer(_txPending, TrafficDirection.Tx);
    }

    private void DrainPending(List<byte> pending, TrafficDirection direction)
    {
        while (SerialMonitorHelper.TryTakeMessage(pending, out var message))
            AppendRecord(direction, message);
    }

    private void FlushBuffer(List<byte> pending, TrafficDirection direction)
    {
        if (pending.Count == 0)
            return;

        AppendRecord(direction, pending.ToArray());
        pending.Clear();
    }

    private void AppendRecord(TrafficDirection direction, byte[] message)
    {
        _records.Add(new TrafficRecord(DateTime.Now, direction, message));
        while (_records.Count > MaxEntries)
            _records.RemoveAt(0);

        while (DisplayLines.Count >= MaxEntries)
            DisplayLines.RemoveAt(0);

        DisplayLines.Add(FormatRecord(_records[^1]));
    }

    private void RebuildDisplay()
    {
        DisplayLines.Clear();
        foreach (var r in _records)
            DisplayLines.Add(FormatRecord(r));

        OnPropertyChanged(nameof(DisplayModeLabel));
    }

    private string FormatRecord(TrafficRecord r)
    {
        var dir = r.Direction == TrafficDirection.Tx ? "TX→" : "RX←";
        var payload = IsHexDisplay
            ? TrafficFormatter.FormatHex(r.Data)
            : TrafficFormatter.FormatText(r.Data);

        return $"[{r.Timestamp:HH:mm:ss.fff}] {dir}  {payload}";
    }

    private void Clear()
    {
        _records.Clear();
        _rxPending.Clear();
        _txPending.Clear();
        DisplayLines.Clear();
    }

    private void Save()
    {
        var dialog = new SaveFileDialog
        {
            Filter = "文本文件 (*.txt)|*.txt",
            FileName = $"serial-monitor-{DateTime.Now:yyyyMMdd-HHmmss}.txt"
        };

        if (dialog.ShowDialog() != true)
            return;

        File.WriteAllLines(dialog.FileName, DisplayLines);
    }

    private sealed record TrafficRecord(DateTime Timestamp, TrafficDirection Direction, byte[] Data);
}
