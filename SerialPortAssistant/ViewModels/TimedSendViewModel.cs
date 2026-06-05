using System.Windows.Threading;

namespace SerialPortAssistant.ViewModels;

public sealed class TimedSendViewModel : ViewModelBase, IDisposable
{
    private const int MinIntervalMs = 100;
    private const int MaxIntervalMs = 3_600_000;

    private readonly Func<bool> _trySend;
    private readonly Func<bool> _isConnected;
    private readonly DispatcherTimer _timer;

    private string _intervalMs = "1000";
    private bool _isRunning;
    private int _sendCount;

    public TimedSendViewModel(Func<bool> trySend, Func<bool> isConnected)
    {
        _trySend = trySend;
        _isConnected = isConnected;
        _timer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(1000) };
        _timer.Tick += (_, _) => OnTick();

        ToggleCommand = new RelayCommand(Toggle, () => _isRunning || _isConnected());
    }

    public string IntervalMs
    {
        get => _intervalMs;
        set => SetProperty(ref _intervalMs, value);
    }

    public bool IsRunning
    {
        get => _isRunning;
        private set
        {
            if (!SetProperty(ref _isRunning, value))
                return;
            OnPropertyChanged(nameof(ToggleButtonText));
            OnPropertyChanged(nameof(CanEditInterval));
            OnPropertyChanged(nameof(StatusText));
            ToggleCommand.RaiseCanExecuteChanged();
        }
    }

    public bool CanEditInterval => !IsRunning;

    public string ToggleButtonText => IsRunning ? "停止" : "开始";

    public string StatusText =>
        SendCount > 0 ? $"已发 {SendCount} 次" : string.Empty;

    public int SendCount
    {
        get => _sendCount;
        private set
        {
            if (!SetProperty(ref _sendCount, value))
                return;
            OnPropertyChanged(nameof(StatusText));
        }
    }

    public RelayCommand ToggleCommand { get; }

    public void Stop()
    {
        if (!IsRunning)
            return;

        _timer.Stop();
        IsRunning = false;
    }

    public void NotifyConnectionChanged()
    {
        if (!_isConnected())
            Stop();
        ToggleCommand.RaiseCanExecuteChanged();
    }

    private void Toggle()
    {
        if (IsRunning)
        {
            Stop();
            return;
        }

        if (!TryParseInterval(out var ms))
            return;

        SendCount = 0;
        _timer.Interval = TimeSpan.FromMilliseconds(ms);
        IsRunning = true;
        OnTick();
        _timer.Start();
    }

    private void OnTick()
    {
        if (!_isConnected())
        {
            Stop();
            return;
        }

        if (!_trySend())
            Stop();
        else
            SendCount++;
    }

    private bool TryParseInterval(out int ms)
    {
        ms = 0;
        if (!int.TryParse(IntervalMs.Trim(), out ms) || ms < MinIntervalMs || ms > MaxIntervalMs)
        {
            System.Windows.MessageBox.Show(
                $"间隔须为 {MinIntervalMs}～{MaxIntervalMs} 的整数（毫秒）。",
                "定时发送",
                System.Windows.MessageBoxButton.OK,
                System.Windows.MessageBoxImage.Information);
            return false;
        }

        return true;
    }

    public void Dispose() => _timer.Stop();
}
