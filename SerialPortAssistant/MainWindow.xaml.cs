using System.Collections.Specialized;
using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;
using SerialPortAssistant.Helpers;
using SerialPortAssistant.Models;
using SerialPortAssistant.ViewModels;

namespace SerialPortAssistant;

public partial class MainWindow : Window
{
    private readonly MainViewModel _viewModel;

    public MainWindow()
    {
        InitializeComponent();
        Title = AppVersion.WindowTitle("串口助手");
        _viewModel = new MainViewModel();
        DataContext = _viewModel;

        _viewModel.Chart.PlotRefreshRequested += RefreshPlot;
        _viewModel.Chart.PropertyChanged += OnChartPropertyChanged;
        _viewModel.Log.Entries.CollectionChanged += OnLogCollectionChanged;
        _viewModel.Ack.Entries.CollectionChanged += OnAckCollectionChanged;
        _viewModel.Traffic.DisplayLines.CollectionChanged += OnTrafficCollectionChanged;
        _viewModel.PropertyChanged += OnViewModelPropertyChanged;

        ApplyWpfChartLabels();
        RefreshPlot();
        UpdateConnectionUi();
        UpdatePauseButton();
        UpdateStats();
        InputBindings.Add(new System.Windows.Input.KeyBinding(
            _viewModel.OpenSettingsCommand,
            new System.Windows.Input.KeyGesture(System.Windows.Input.Key.OemComma, System.Windows.Input.ModifierKeys.Control)));
        InputBindings.Add(new System.Windows.Input.KeyBinding(
            _viewModel.OpenHelpCommand,
            new System.Windows.Input.KeyGesture(System.Windows.Input.Key.F1)));
    }

    private void ApplyWpfChartLabels()
    {
        PlotTitleLabel.Text = ChartPlotHelper.PlotTitle;
        PlotYAxisLabel.Text = ChartPlotHelper.YAxisLabel;
        PlotXAxisLabel.Text = ChartPlotHelper.XAxisLabel;
    }

    private void RefreshPlot()
    {
        var (x, ys, channelCount) = _viewModel.Chart.GetPlotData();
        var names = _viewModel.Chart.GetChannelNames();
        var active = channelCount > 0 ? channelCount : _viewModel.Chart.ActiveChannelCount;
        if (active == 0)
            active = 1;

        DataChart.UpdatePlot(x, ys, active, names);
    }

    private void OnChartPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName is nameof(ChartViewModel.StatsText) or nameof(ChartViewModel.IsPaused))
        {
            Dispatcher.Invoke(() =>
            {
                UpdateStats();
                UpdatePauseButton();
            });
        }
    }

    private void OnLogCollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {
        if (!_viewModel.Log.AutoScroll || !ShouldScrollForChange(e))
            return;

        ListBoxScrollHelper.ScrollToLatest(LogListBox);
    }

    private void OnAckCollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {
        if (!ShouldScrollForChange(e))
            return;

        ListBoxScrollHelper.ScrollToLatest(AckListBox);
    }

    private void OnTrafficCollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {
        if (!_viewModel.Traffic.AutoScroll || !ShouldScrollForChange(e))
            return;

        ListBoxScrollHelper.ScrollToLatest(TrafficListBox);
    }

    private static bool ShouldScrollForChange(NotifyCollectionChangedEventArgs e) =>
        e.Action is NotifyCollectionChangedAction.Add or NotifyCollectionChangedAction.Reset;

    private void OnViewModelPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName is nameof(MainViewModel.IsConnected)
            or nameof(MainViewModel.UseCustomBaudRate)
            or nameof(MainViewModel.ConnectButtonText)
            or nameof(MainViewModel.ConnectionStatusLabel)
            or nameof(MainViewModel.ConnectionDetailText)
            or nameof(MainViewModel.StatusMessage))
        {
            Dispatcher.Invoke(UpdateConnectionUi);
        }
    }

    private void UpdateConnectionUi()
    {
        var connected = _viewModel.IsConnected;
        PortCombo.IsEnabled = !connected;
        BaudCombo.IsEnabled = !connected && !_viewModel.UseCustomBaudRate;
        ParityCombo.IsEnabled = !connected;
        DataBitsCombo.IsEnabled = !connected;
        StopBitsCombo.IsEnabled = !connected;
    }

    private void UpdatePauseButton() =>
        PauseButton.Content = _viewModel.Chart.IsPaused ? "继续" : "暂停";

    private void UpdateStats() =>
        StatsText.Text = _viewModel.Chart.StatsText;

    private void PauseButton_Click(object sender, RoutedEventArgs e) =>
        _viewModel.Chart.IsPaused = !_viewModel.Chart.IsPaused;

    private void QuickCommand_Click(object sender, RoutedEventArgs e)
    {
        if (sender is Button { Tag: CommandOpcodeDefinition opcode })
            _viewModel.Commands.ApplyOpcode(opcode.Opcode);
    }

    protected override void OnClosed(EventArgs e)
    {
        _viewModel.Chart.PlotRefreshRequested -= RefreshPlot;
        _viewModel.Dispose();
        base.OnClosed(e);
    }
}
