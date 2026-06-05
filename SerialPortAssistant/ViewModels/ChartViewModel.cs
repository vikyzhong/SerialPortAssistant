using System.IO;
using System.Text;
using System.Windows;
using System.Windows.Threading;
using Microsoft.Win32;
using SerialPortAssistant.Helpers;
using SerialPortAssistant.Models;

namespace SerialPortAssistant.ViewModels;

public sealed class ChartViewModel : ViewModelBase, IDisposable
{
    /// <summary>达到该点数后清空曲线并从 X=0 重新开始。</summary>
    public const int MaxPoints = 100_000;
    private const int RefreshBatchSize = 30;

    private readonly Func<AppSettings> _getSettings;
    private readonly List<double> _xValues = new(MaxPoints);
    private readonly List<double>[] _yValues =
    [
        new(MaxPoints),
        new(MaxPoints),
        new(MaxPoints)
    ];
    private readonly DispatcherTimer _refreshTimer;

    private int _pendingPoints;
    private bool _isPaused;
    private readonly ushort[] _currentChannels = new ushort[DataChannels.MaxCount];
    private int _activeChannelCount;
    private double _pointsPerSecond;
    private int _pointCountSinceRateCalc;
    private DateTime _lastRateCalc = DateTime.UtcNow;
    private int _wrapCount;

    public event Action? PlotRefreshRequested;

    public bool IsPaused
    {
        get => _isPaused;
        set => SetProperty(ref _isPaused, value);
    }

    public int ActiveChannelCount
    {
        get => _activeChannelCount;
        private set => SetProperty(ref _activeChannelCount, value);
    }

    public double PointsPerSecond
    {
        get => _pointsPerSecond;
        private set => SetProperty(ref _pointsPerSecond, value);
    }

    public string StatsText
    {
        get
        {
            var names = _getSettings().DataChannels.ToArray();
            var parts = new List<string>();
            for (var i = 0; i < ActiveChannelCount; i++)
            {
                var v = _currentChannels[i];
                parts.Add($"{names[i]}: {v} (0x{v:X4})");
            }

            var pointCount = 0;
            lock (_xValues)
                pointCount = _xValues.Count;

            var indexRange = pointCount == 0 ? "X: —" : $"X: 0~{pointCount - 1}";
            var wrapNote = _wrapCount > 0 ? $"  |  已回绕: {_wrapCount} 次" : string.Empty;

            return string.Join("  |  ", parts) +
                   $"  |  {indexRange}  |  速率: {PointsPerSecond:F0} 点/秒  |  点数: {pointCount}/{MaxPoints}{wrapNote}";
        }
    }

    public RelayCommand ClearCommand { get; }
    public RelayCommand ExportCsvCommand { get; }

    public ChartViewModel(Func<AppSettings> getSettings)
    {
        _getSettings = getSettings;
        ClearCommand = new RelayCommand(Clear);
        ExportCsvCommand = new RelayCommand(ExportCsv);

        _refreshTimer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(50) };
        _refreshTimer.Tick += (_, _) => FlushPending();
        _refreshTimer.Start();
    }

    public void HandleParsedLine(ParsedLine line)
    {
        if (IsPaused || line.Kind != LineKind.Data || line.Channels.Length == 0)
            return;

        var count = Math.Min(line.Channels.Length, DataChannels.MaxCount);

        lock (_xValues)
        {
            if (_xValues.Count >= MaxPoints)
                RestartSegment();

            _xValues.Add(_xValues.Count);

            for (var i = 0; i < DataChannels.MaxCount; i++)
            {
                double y;
                if (i < count)
                    y = line.Channels[i];
                else if (_yValues[i].Count > 0)
                    y = _yValues[i][^1];
                else
                    y = double.NaN;

                _yValues[i].Add(y);
            }
        }

        for (var i = 0; i < DataChannels.MaxCount; i++)
            _currentChannels[i] = i < count ? line.Channels[i] : (ushort)0;

        ActiveChannelCount = Math.Max(ActiveChannelCount, count);
        _pendingPoints++;
        _pointCountSinceRateCalc++;

        if (_pendingPoints >= RefreshBatchSize)
            FlushPending();
    }

    public (double[] X, double[][] Y, int ChannelCount) GetPlotData()
    {
        lock (_xValues)
        {
            var count = ActiveChannelCount;
            if (count == 0)
                count = DataChannels.MaxCount;

            var ys = new double[DataChannels.MaxCount][];
            for (var i = 0; i < DataChannels.MaxCount; i++)
                ys[i] = _yValues[i].ToArray();

            return (_xValues.ToArray(), ys, count);
        }
    }

    public string[] GetChannelNames() => _getSettings().DataChannels.ToArray();

    private void RestartSegment()
    {
        _wrapCount++;
        _xValues.Clear();
        foreach (var y in _yValues)
            y.Clear();
    }

    private void FlushPending()
    {
        if (_pendingPoints == 0)
            return;

        _pendingPoints = 0;
        UpdateRate();
        OnPropertyChanged(nameof(StatsText));
        PlotRefreshRequested?.Invoke();
    }

    private void UpdateRate()
    {
        var now = DateTime.UtcNow;
        var elapsed = (now - _lastRateCalc).TotalSeconds;
        if (elapsed < 0.5)
            return;

        PointsPerSecond = _pointCountSinceRateCalc / elapsed;
        _pointCountSinceRateCalc = 0;
        _lastRateCalc = now;
    }

    private void Clear()
    {
        lock (_xValues)
        {
            _xValues.Clear();
            foreach (var y in _yValues)
                y.Clear();
        }

        _wrapCount = 0;
        _pendingPoints = 0;
        Array.Clear(_currentChannels, 0, _currentChannels.Length);
        ActiveChannelCount = 0;
        PointsPerSecond = 0;
        OnPropertyChanged(nameof(StatsText));
        PlotRefreshRequested?.Invoke();
    }

    public void ReloadChannelNames() => OnPropertyChanged(nameof(StatsText));

    private void ExportCsv()
    {
        int count;
        double[] x;
        double[][] y;
        int channels;

        lock (_xValues)
        {
            count = _xValues.Count;
            if (count == 0)
            {
                MessageBox.Show("当前曲线没有数据点。", "导出 CSV", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            if (count > 50_000)
            {
                var confirm = MessageBox.Show(
                    $"将导出 {count:N0} 行数据，文件可能较大。是否继续？",
                    "导出 CSV",
                    MessageBoxButton.YesNo,
                    MessageBoxImage.Question);
                if (confirm != MessageBoxResult.Yes)
                    return;
            }

            channels = ActiveChannelCount > 0 ? ActiveChannelCount : DataChannels.MaxCount;
            x = _xValues.ToArray();
            y = new double[DataChannels.MaxCount][];
            for (var i = 0; i < DataChannels.MaxCount; i++)
                y[i] = _yValues[i].ToArray();
        }

        string csv;
        try
        {
            csv = ChartCsvExporter.BuildCsv(x, y, channels, GetChannelNames());
        }
        catch (Exception ex)
        {
            MessageBox.Show(ex.Message, "导出 CSV", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        var dialog = new SaveFileDialog
        {
            Filter = "CSV (*.csv)|*.csv|所有文件 (*.*)|*.*",
            FileName = $"chart-data-{DateTime.Now:yyyyMMdd-HHmmss}.csv"
        };

        if (dialog.ShowDialog() != true)
            return;

        try
        {
            File.WriteAllText(dialog.FileName, csv, new UTF8Encoding(encoderShouldEmitUTF8Identifier: true));
            MessageBox.Show($"已保存 {count:N0} 行。\n{dialog.FileName}", "导出 CSV", MessageBoxButton.OK,
                MessageBoxImage.Information);
        }
        catch (Exception ex)
        {
            MessageBox.Show(ex.Message, "导出失败", MessageBoxButton.OK, MessageBoxImage.Warning);
        }
    }

    public void Dispose() => _refreshTimer.Stop();
}
