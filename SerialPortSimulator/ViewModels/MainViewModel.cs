using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Threading;
using SerialPortSimulator.Helpers;
using SerialPortSimulator.Models;
using SerialPortSimulator.Services;

namespace SerialPortSimulator.ViewModels;

public sealed class MainViewModel : ViewModelBase, IDisposable
{
    private readonly SerialBridge _serial = new();
    private readonly TestDataGenerator _generator = new();
    private readonly DispatcherTimer _dataTimer = new();
    private readonly DispatcherTimer _logTimer = new();
    private readonly List<byte> _rxPending = new();
    private readonly List<byte> _txPending = new();

    private string _selectedPort = string.Empty;
    private int _baudRate = 115200;
    private bool _isConnected;
    private bool _autoData;
    private bool _autoLog;
    private bool _autoAck = true;
    private bool _threeChannels = true;
    private bool _useDatbBinary;
    private bool _logUseLevelByte = true;
    private int _dataIntervalMs = 100;
    private int _logIntervalMs = 2000;
    private string _manualSend = "LOG:1,手动测试消息";
    private string _rawSend = "48 65 6C 6C 6F";
    private bool _rawSendAsHex = true;
    private bool _rawSendAppendNewline = true;
    private bool _monitorHexDisplay = true;
    private string _statusText = "未连接 — 请使用虚拟串口对的另一端";

    public ObservableCollection<string> Ports { get; } = new();
    public ObservableCollection<string> TrafficLog { get; } = new();
    public ObservableCollection<CmdRule> CmdRules { get; } = new(CmdResponder.CreateDefaultRules());

    public string SelectedPort
    {
        get => _selectedPort;
        set => SetProperty(ref _selectedPort, value);
    }

    public int BaudRate
    {
        get => _baudRate;
        set => SetProperty(ref _baudRate, value);
    }

    public bool IsConnected
    {
        get => _isConnected;
        set
        {
            if (SetProperty(ref _isConnected, value))
            {
                ConnectCommand.RaiseCanExecuteChanged();
                SendManualCommand.RaiseCanExecuteChanged();
                SendDataOnceCommand.RaiseCanExecuteChanged();
                SendLogOnceCommand.RaiseCanExecuteChanged();
                SendRawCommand.RaiseCanExecuteChanged();
                OnPropertyChanged(nameof(ConnectButtonText));
                OnPropertyChanged(nameof(ConnectionStatusLabel));
            }
        }
    }

    public string ConnectButtonText => IsConnected ? "断开" : "打开";
    public string ConnectionStatusLabel => IsConnected ? "● 模拟器已连接" : "○ 模拟器未连接";

    public bool AutoData
    {
        get => _autoData;
        set { if (SetProperty(ref _autoData, value)) UpdateTimers(); }
    }

    public bool AutoLog
    {
        get => _autoLog;
        set { if (SetProperty(ref _autoLog, value)) UpdateTimers(); }
    }

    public bool AutoAck
    {
        get => _autoAck;
        set => SetProperty(ref _autoAck, value);
    }

    public bool ThreeChannels
    {
        get => _threeChannels;
        set => SetProperty(ref _threeChannels, value);
    }

    public bool UseDatbBinary
    {
        get => _useDatbBinary;
        set => SetProperty(ref _useDatbBinary, value);
    }

    public bool LogUseLevelByte
    {
        get => _logUseLevelByte;
        set => SetProperty(ref _logUseLevelByte, value);
    }

    public int DataIntervalMs
    {
        get => _dataIntervalMs;
        set { if (SetProperty(ref _dataIntervalMs, value)) _dataTimer.Interval = TimeSpan.FromMilliseconds(Math.Max(20, value)); }
    }

    public int LogIntervalMs
    {
        get => _logIntervalMs;
        set { if (SetProperty(ref _logIntervalMs, value)) _logTimer.Interval = TimeSpan.FromMilliseconds(Math.Max(200, value)); }
    }

    public string ManualSend
    {
        get => _manualSend;
        set => SetProperty(ref _manualSend, value);
    }

    public string RawSend
    {
        get => _rawSend;
        set => SetProperty(ref _rawSend, value);
    }

    public bool RawSendAsHex
    {
        get => _rawSendAsHex;
        set => SetProperty(ref _rawSendAsHex, value);
    }

    public bool RawSendAppendNewline
    {
        get => _rawSendAppendNewline;
        set => SetProperty(ref _rawSendAppendNewline, value);
    }

    public bool MonitorHexDisplay
    {
        get => _monitorHexDisplay;
        set
        {
            if (SetProperty(ref _monitorHexDisplay, value))
                RebuildMonitorDisplay();
        }
    }

    public string MonitorDisplayModeLabel => MonitorHexDisplay ? "HEX" : "字符串";

    public string StatusText
    {
        get => _statusText;
        set => SetProperty(ref _statusText, value);
    }

    public int[] BaudRates { get; } = [9600, 115200, 921600, 1000000];

    public RelayCommand RefreshPortsCommand { get; }
    public RelayCommand ConnectCommand { get; }
    public RelayCommand SendManualCommand { get; }
    public RelayCommand SendDataOnceCommand { get; }
    public RelayCommand SendLogOnceCommand { get; }
    public RelayCommand SendRawCommand { get; }
    public RelayCommand ToggleMonitorDisplayCommand { get; }
    public RelayCommand ClearLogCommand { get; }
    public RelayCommand ResetRulesCommand { get; }

    private readonly List<(DateTime Time, TransferDirection Dir, byte[] Data)> _monitorRecords = new();

    public MainViewModel()
    {
        _dataTimer.Tick += (_, _) => SendGeneratedData();
        _logTimer.Tick += (_, _) => SendGeneratedLog();
        _dataTimer.Interval = TimeSpan.FromMilliseconds(_dataIntervalMs);
        _logTimer.Interval = TimeSpan.FromMilliseconds(_logIntervalMs);

        RefreshPortsCommand = new RelayCommand(RefreshPorts);
        ConnectCommand = new RelayCommand(ToggleConnection, () => !string.IsNullOrWhiteSpace(SelectedPort) || IsConnected);
        SendManualCommand = new RelayCommand(() => SendLine(ManualSend), () => IsConnected);
        SendDataOnceCommand = new RelayCommand(SendGeneratedData, () => IsConnected);
        SendLogOnceCommand = new RelayCommand(SendGeneratedLog, () => IsConnected);
        SendRawCommand = new RelayCommand(SendRaw, () => IsConnected);
        ToggleMonitorDisplayCommand = new RelayCommand(() => MonitorHexDisplay = !MonitorHexDisplay);
        ClearLogCommand = new RelayCommand(ClearMonitor);
        ResetRulesCommand = new RelayCommand(ResetRules);

        _serial.CommandReceived += OnCommandReceived;
        _serial.BytesTransferred += OnBytesTransferred;
        _serial.ErrorOccurred += (_, msg) => Application.Current.Dispatcher.Invoke(() => StatusText = $"错误: {msg}");

        RefreshPorts();
    }

    private void RefreshPorts()
    {
        Ports.Clear();
        foreach (var p in SerialBridge.GetPortNames().OrderBy(x => x))
            Ports.Add(p);

        if (Ports.Count > 0 && (string.IsNullOrEmpty(SelectedPort) || !Ports.Contains(SelectedPort)))
            SelectedPort = Ports[0];
    }

    private void ToggleConnection()
    {
        if (IsConnected)
        {
            _dataTimer.Stop();
            _logTimer.Stop();
            FlushMonitorPending();
            _serial.Close();
            IsConnected = false;
            StatusText = $"已断开 {SelectedPort}";
            AddSystemLine("模拟器已断开");
            return;
        }

        try
        {
            _serial.Open(SelectedPort, BaudRate);
            IsConnected = true;
            StatusText = $"已连接 {SelectedPort} @ {BaudRate} — 等待主机 AT+/CMD";
            AddSystemLine($"模拟器已连接 {SelectedPort} @ {BaudRate}");
            UpdateTimers();
        }
        catch (Exception ex)
        {
            MessageBox.Show(ex.Message, "连接失败", MessageBoxButton.OK, MessageBoxImage.Warning);
        }
    }

    private void UpdateTimers()
    {
        if (!IsConnected)
        {
            _dataTimer.Stop();
            _logTimer.Stop();
            return;
        }

        if (AutoData) _dataTimer.Start(); else _dataTimer.Stop();
        if (AutoLog) _logTimer.Start(); else _logTimer.Stop();
    }

    private void SendGeneratedData()
    {
        if (!IsConnected) return;

        if (UseDatbBinary)
        {
            try
            {
                _serial.WriteRaw(_generator.NextDatbFrame(ThreeChannels));
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "发送失败", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
            return;
        }

        SendLine(_generator.NextDataLine(ThreeChannels));
    }

    private void SendGeneratedLog()
    {
        if (!IsConnected) return;
        SendLine(_generator.NextLogLine(LogUseLevelByte));
    }

    private void SendLine(string line)
    {
        try
        {
            _serial.WriteLine(line);
        }
        catch (Exception ex)
        {
            MessageBox.Show(ex.Message, "发送失败", MessageBoxButton.OK, MessageBoxImage.Warning);
        }
    }

    private void SendRaw()
    {
        try
        {
            byte[] payload;
            if (RawSendAsHex)
            {
                payload = SerialMonitorHelper.ParseHexPayload(RawSend);
            }
            else
            {
                payload = System.Text.Encoding.UTF8.GetBytes(RawSend);
            }

            if (RawSendAppendNewline)
            {
                var withNl = new byte[payload.Length + 1];
                Array.Copy(payload, withNl, payload.Length);
                withNl[^1] = (byte)'\n';
                payload = withNl;
            }

            if (payload.Length == 0)
            {
                MessageBox.Show("发送内容为空。", "提示", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            _serial.WriteRaw(payload);
        }
        catch (Exception ex)
        {
            MessageBox.Show(ex.Message, "发送失败", MessageBoxButton.OK, MessageBoxImage.Warning);
        }
    }

    private void OnBytesTransferred(object? sender, TransferEventArgs e) =>
        Application.Current.Dispatcher.Invoke(() => AppendMonitorBytes(e.Direction, e.Data));

    private void OnCommandReceived(object? sender, HostCommand command)
    {
        Application.Current.Dispatcher.Invoke(() =>
        {
            if (!AutoAck) return;

            var responder = new CmdResponder(CmdRules);
            var response = responder.TryRespond(command);
            if (response == null) return;

            _ = Task.Run(async () =>
            {
                await Task.Delay(30);
                Application.Current.Dispatcher.Invoke(() =>
                {
                    if (response.BinaryFrame != null)
                        SendBinary(response.BinaryFrame);
                    else if (response.TextLines is { Count: > 0 } lines)
                    {
                        foreach (var textLine in lines)
                            SendLine(textLine.TrimEnd('\r', '\n'));
                    }
                    else if (!string.IsNullOrEmpty(response.TextLine))
                        SendLine(response.TextLine);
                });
            });
        });
    }

    private void SendBinary(byte[] frame)
    {
        try
        {
            _serial.WriteRaw(frame);
        }
        catch (Exception ex)
        {
            MessageBox.Show(ex.Message, "发送失败", MessageBoxButton.OK, MessageBoxImage.Warning);
        }
    }

    private void AppendMonitorBytes(TransferDirection direction, byte[] data)
    {
        if (data.Length == 0) return;

        var pending = direction == TransferDirection.Rx ? _rxPending : _txPending;
        pending.AddRange(data);
        DrainPending(pending, direction);
    }

    private void DrainPending(List<byte> pending, TransferDirection direction)
    {
        while (SerialMonitorHelper.TryTakeMessage(pending, out var message))
            AppendMonitorRecord(direction, message);
    }

    private void FlushMonitorPending()
    {
        FlushPendingBuffer(_rxPending, TransferDirection.Rx);
        FlushPendingBuffer(_txPending, TransferDirection.Tx);
    }

    private void FlushPendingBuffer(List<byte> pending, TransferDirection direction)
    {
        if (pending.Count == 0) return;
        AppendMonitorRecord(direction, pending.ToArray());
        pending.Clear();
    }

    private void AppendMonitorRecord(TransferDirection direction, byte[] message)
    {
        _monitorRecords.Add((DateTime.Now, direction, message));
        while (_monitorRecords.Count > 2000)
            _monitorRecords.RemoveAt(0);

        TrafficLog.Add(FormatMonitorLine(DateTime.Now, direction, message));
        while (TrafficLog.Count > 2000)
            TrafficLog.RemoveAt(0);
    }

    private void RebuildMonitorDisplay()
    {
        TrafficLog.Clear();
        foreach (var (time, dir, data) in _monitorRecords)
            TrafficLog.Add(FormatMonitorLine(time, dir, data));
    }

    private string FormatMonitorLine(DateTime time, TransferDirection direction, byte[] data)
    {
        var label = direction == TransferDirection.Tx ? "TX→" : "RX←";
        var payload = MonitorHexDisplay
            ? SerialMonitorHelper.FormatHex(data)
            : SerialMonitorHelper.FormatText(data);
        return $"[{time:HH:mm:ss.fff}] {label}  {payload}";
    }

    private void AddSystemLine(string text)
    {
        TrafficLog.Add($"[{DateTime.Now:HH:mm:ss.fff}] SYS  {text}");
        while (TrafficLog.Count > 2000)
            TrafficLog.RemoveAt(0);
    }

    private void ClearMonitor()
    {
        _monitorRecords.Clear();
        _rxPending.Clear();
        _txPending.Clear();
        TrafficLog.Clear();
    }

    private void ResetRules()
    {
        CmdRules.Clear();
        foreach (var r in CmdResponder.CreateDefaultRules())
            CmdRules.Add(r);
    }

    public void Dispose()
    {
        _dataTimer.Stop();
        _logTimer.Stop();
        _serial.Dispose();
    }
}
