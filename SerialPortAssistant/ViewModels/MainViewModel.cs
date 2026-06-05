using System.Collections.ObjectModel;
using System.IO.Ports;
using System.Windows;
using SerialPortAssistant.Helpers;
using SerialPortAssistant.Models;
using SerialPortAssistant.Services;
using SerialPortAssistant.Views;

namespace SerialPortAssistant.ViewModels;

public sealed class MainViewModel : ViewModelBase, IDisposable
{
    private readonly SerialPortService _serialService;
    private readonly SettingsService _settingsService = new();
    private readonly LineParser _lineParser;

    private string _selectedPort = string.Empty;
    private int _selectedBaudRate = 115200;
    private string _customBaudRate = "115200";
    private bool _useCustomBaudRate;
    private string _statusMessage = "未连接";
    private bool _isConnected;
    private Parity _parity = Parity.None;
    private int _dataBits = 8;
    private StopBits _stopBits = StopBits.One;
    private Handshake _handshake = Handshake.None;
    private bool _dtrEnable;
    private bool _rtsEnable;

    public ChartViewModel Chart { get; }
    public LogViewModel Log { get; }
    public AckViewModel Ack { get; }
    public CommandViewModel Commands { get; }
    public TrafficMonitorViewModel Traffic { get; }
    public RawSendViewModel RawSend { get; }
    public TimedSendViewModel TimedSend { get; }

    public ObservableCollection<string> Ports { get; } = new();
    public ObservableCollection<int> BaudRates { get; } = new(SerialSettings.PresetBaudRates);

    public string SelectedPort
    {
        get => _selectedPort;
        set => SetProperty(ref _selectedPort, value);
    }

    public int SelectedBaudRate
    {
        get => _selectedBaudRate;
        set => SetProperty(ref _selectedBaudRate, value);
    }

    public string CustomBaudRate
    {
        get => _customBaudRate;
        set => SetProperty(ref _customBaudRate, value);
    }

    public bool UseCustomBaudRate
    {
        get => _useCustomBaudRate;
        set => SetProperty(ref _useCustomBaudRate, value);
    }

    public string StatusMessage
    {
        get => _statusMessage;
        set
        {
            if (SetProperty(ref _statusMessage, value))
                OnPropertyChanged(nameof(ConnectionDetailText));
        }
    }

    public bool IsConnected
    {
        get => _isConnected;
        set
        {
            if (SetProperty(ref _isConnected, value))
            {
                ConnectCommand.RaiseCanExecuteChanged();
                Commands.SendCommand.RaiseCanExecuteChanged();
                RawSend.NotifyConnectionChanged();
                TimedSend.NotifyConnectionChanged();
                OnPropertyChanged(nameof(ConnectButtonText));
                OnPropertyChanged(nameof(ConnectionStatusLabel));
                OnPropertyChanged(nameof(ConnectionDetailText));
            }
        }
    }

    public string ConnectButtonText => IsConnected ? "断开" : "打开";

    public string ConnectionStatusLabel => IsConnected ? "已连接" : "未连接";

    public string ConnectionDetailText =>
        IsConnected ? StatusMessage : (string.IsNullOrWhiteSpace(SelectedPort) ? "请选择 COM 口" : StatusMessage);

    public Parity Parity
    {
        get => _parity;
        set => SetProperty(ref _parity, value);
    }

    public int DataBits
    {
        get => _dataBits;
        set => SetProperty(ref _dataBits, value);
    }

    public StopBits StopBits
    {
        get => _stopBits;
        set => SetProperty(ref _stopBits, value);
    }

    public Handshake Handshake
    {
        get => _handshake;
        set
        {
            if (!SetProperty(ref _handshake, value))
                return;
            ApplyLineSignalsIfConnected();
            PersistSerialPortPreferences();
        }
    }

    public bool DtrEnable
    {
        get => _dtrEnable;
        set
        {
            if (!SetProperty(ref _dtrEnable, value))
                return;
            ApplyLineSignalsIfConnected();
            PersistSerialPortPreferences();
        }
    }

    public bool RtsEnable
    {
        get => _rtsEnable;
        set
        {
            if (!SetProperty(ref _rtsEnable, value))
                return;
            ApplyLineSignalsIfConnected();
            PersistSerialPortPreferences();
        }
    }

    public Array ParityOptions => Enum.GetValues(typeof(Parity));
    public Array StopBitsOptions => Enum.GetValues(typeof(StopBits));
    public Array HandshakeOptions => Enum.GetValues(typeof(Handshake));
    public int[] DataBitsOptions { get; } = [5, 6, 7, 8];

    public RelayCommand RefreshPortsCommand { get; }
    public RelayCommand ConnectCommand { get; }
    public RelayCommand OpenSettingsCommand { get; }
    public RelayCommand OpenHelpCommand { get; }
    public RelayCommand OpenProtocolOverviewCommand { get; }
    public RelayCommand OpenAboutCommand { get; }

    public MainViewModel()
    {
        _lineParser = new LineParser(() => _settingsService.Current);
        _serialService = new SerialPortService(() => _settingsService.Current);
        _serialService.ConfigureParser(_lineParser);

        Chart = new ChartViewModel(() => _settingsService.Current);
        Log = new LogViewModel(() => _settingsService.Current, s => _settingsService.Save(s));
        Ack = new AckViewModel(() => _settingsService.Current);
        Commands = new CommandViewModel(_settingsService, SendCommandBundle, () => IsConnected);
        TimedSend = new TimedSendViewModel(() => Commands.TrySend(), () => IsConnected);
        Traffic = new TrafficMonitorViewModel();
        RawSend = new RawSendViewModel(_serialService, () => IsConnected);

        RefreshPortsCommand = new RelayCommand(RefreshPorts);
        ConnectCommand = new RelayCommand(ToggleConnection, () => !string.IsNullOrWhiteSpace(SelectedPort) || IsConnected);
        OpenSettingsCommand = new RelayCommand(OpenSettings);
        OpenHelpCommand = new RelayCommand(OpenHelp);
        OpenProtocolOverviewCommand = new RelayCommand(OpenProtocolOverview);
        OpenAboutCommand = new RelayCommand(OpenAbout);

        _serialService.LineReceived += OnLineReceived;
        _serialService.BytesTransferred += (_, e) => Traffic.Add(e.Direction, e.Data);
        _serialService.ErrorOccurred += (_, msg) =>
            Application.Current.Dispatcher.BeginInvoke(() =>
            {
                if (!IsConnected)
                    return;
                StatusMessage = $"错误: {msg}";
            });

        LoadSerialPortPreferences();
        RefreshPorts();
    }

    private void LoadSerialPortPreferences()
    {
        var sp = _settingsService.Current.SerialPort;
        _handshake = sp.Handshake;
        _dtrEnable = sp.DtrEnable;
        _rtsEnable = sp.RtsEnable;
        OnPropertyChanged(nameof(Handshake));
        OnPropertyChanged(nameof(DtrEnable));
        OnPropertyChanged(nameof(RtsEnable));
    }

    private void PersistSerialPortPreferences()
    {
        var settings = _settingsService.Current;
        settings.SerialPort.Handshake = Handshake;
        settings.SerialPort.DtrEnable = DtrEnable;
        settings.SerialPort.RtsEnable = RtsEnable;
        _settingsService.Save(settings);
    }

    private void ApplyLineSignalsIfConnected()
    {
        if (!IsConnected)
            return;

        try
        {
            _serialService.ApplyLineSignals(DtrEnable, RtsEnable, Handshake);
        }
        catch (Exception ex)
        {
            MessageBox.Show(ex.Message, "串口线路", MessageBoxButton.OK, MessageBoxImage.Warning);
        }
    }

    private void SendCommandBundle(CommandSendBundle bundle)
    {
        try
        {
            if (bundle.RawFrame != null)
                _serialService.WriteRaw(bundle.RawFrame);
            else if (!string.IsNullOrEmpty(bundle.TextLine))
                _serialService.WriteAtText(bundle.TextLine, _settingsService.Current.AtTextLineEnding);

            Ack.NotifySent(bundle.DisplayText);
        }
        catch (Exception ex)
        {
            MessageBox.Show(ex.Message, "发送失败", MessageBoxButton.OK, MessageBoxImage.Warning);
        }
    }

    private void OpenSettings()
    {
        var window = new SettingsWindow(_settingsService) { Owner = Application.Current.MainWindow };
        if (window.ShowDialog() == true)
        {
            _serialService.ConfigureParser(_lineParser);
            Commands.ReloadFromSettings();
            Log.ReloadLevelStyles();
            Chart.ReloadChannelNames();
        }
    }

    private static void OpenHelp()
    {
        var window = new HelpWindow { Owner = Application.Current.MainWindow };
        window.ShowDialog();
    }

    private static void OpenProtocolOverview()
    {
        var window = new ProtocolOverviewWindow { Owner = Application.Current.MainWindow };
        window.ShowDialog();
    }

    private static void OpenAbout()
    {
        var window = new AboutWindow { Owner = Application.Current.MainWindow };
        window.ShowDialog();
    }

    private void RefreshPorts()
    {
        Ports.Clear();
        foreach (var port in SerialPortService.GetPortNames().OrderBy(p => p))
            Ports.Add(port);

        if (Ports.Count > 0 && (string.IsNullOrEmpty(SelectedPort) || !Ports.Contains(SelectedPort)))
            SelectedPort = Ports[0];
    }

    private void ToggleConnection()
    {
        if (IsConnected)
        {
            var portName = SelectedPort;
            IsConnected = false;
            StatusMessage = $"已断开 {portName}";
            Traffic.FlushPending();
            _serialService.Close();
            return;
        }

        try
        {
            var baud = ResolveBaudRate();
            var settings = new SerialSettings
            {
                PortName = SelectedPort,
                BaudRate = baud,
                Parity = Parity,
                DataBits = DataBits,
                StopBits = StopBits,
                Handshake = Handshake,
                DtrEnable = DtrEnable,
                RtsEnable = RtsEnable
            };

            _serialService.Open(settings);
            IsConnected = true;
            StatusMessage = $"已连接 {SelectedPort} @ {baud}";
        }
        catch (Exception ex)
        {
            MessageBox.Show(ex.Message, "串口打开失败", MessageBoxButton.OK, MessageBoxImage.Warning);
            StatusMessage = "连接失败";
        }
    }

    private int ResolveBaudRate()
    {
        if (UseCustomBaudRate)
        {
            if (!int.TryParse(CustomBaudRate, out var custom) || custom <= 0)
                throw new InvalidOperationException("自定义波特率无效");
            return custom;
        }

        return SelectedBaudRate;
    }

    private void OnLineReceived(object? sender, ParsedLine line)
    {
        Application.Current.Dispatcher.BeginInvoke(() =>
        {
            if (!IsConnected)
                return;

            Chart.HandleParsedLine(line);
            Log.HandleParsedLine(line);
            Ack.HandleParsedLine(line);
        });
    }

    public void Dispose()
    {
        TimedSend.Dispose();
        _serialService.Dispose();
        Chart.Dispose();
    }
}
