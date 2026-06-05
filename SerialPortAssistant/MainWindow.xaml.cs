using System.Collections.Specialized;
using System.ComponentModel;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using ScottPlot.TickGenerators;
using SerialPortAssistant.Helpers;
using SerialPortAssistant.Models;
using SerialPortAssistant.ViewModels;

namespace SerialPortAssistant;

public partial class MainWindow : Window
{
    private static readonly ScottPlot.Color[] ChannelColors =
    [
        ScottPlot.Color.FromHex("#2196F3"),
        ScottPlot.Color.FromHex("#FF5722"),
        ScottPlot.Color.FromHex("#4CAF50")
    ];

    private readonly MainViewModel _viewModel;

    public MainWindow()
    {
        InitializeComponent();
        _viewModel = new MainViewModel();
        DataContext = _viewModel;

        _viewModel.Chart.PlotRefreshRequested += RefreshPlot;
        _viewModel.Chart.PropertyChanged += OnChartPropertyChanged;
        _viewModel.Log.Entries.CollectionChanged += OnLogCollectionChanged;
        _viewModel.Ack.Entries.CollectionChanged += OnAckCollectionChanged;
        _viewModel.Traffic.DisplayLines.CollectionChanged += OnTrafficCollectionChanged;
        _viewModel.PropertyChanged += OnViewModelPropertyChanged;

        ConfigurePlot();
        HookPlotAxisInteraction();
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

    private void ConfigurePlot()
    {
        HideScottPlotAxisTitles();
        ApplyWpfChartLabels();
        ScottPlotFontConfigurator.ApplyToPlot(DataPlot.Plot);
        ConfigureXAxisTicks();
        ApplyDefaultAxisLimits();
        DataPlot.Plot.Grid.MajorLineColor = ScottPlot.Color.FromHex("#E0E0E0");
        DataPlot.Refresh();
    }

    /// <summary>标题与坐标轴中文由 WPF 文本绘制，避免 ScottPlot 缺字显示为方框。</summary>
    private void ApplyWpfChartLabels()
    {
        PlotTitleLabel.Text = ChartPlotHelper.PlotTitle;
        PlotYAxisLabel.Text = ChartPlotHelper.YAxisLabel;
        PlotXAxisLabel.Text = ChartPlotHelper.XAxisLabel;
    }

    private void HideScottPlotAxisTitles()
    {
        var plot = DataPlot.Plot;
        plot.Axes.Title.IsVisible = false;
        plot.Axes.Left.Label.IsVisible = false;
        plot.Axes.Bottom.Label.IsVisible = false;
    }

    private void HookPlotAxisInteraction()
    {
        void RefreshAxisAfterInteraction()
        {
            ClampAxisLimitsToBounds();
            ConfigureXAxisTicks();
            DataPlot.Refresh();
        }

        DataPlot.MouseWheel += (_, _) => RefreshAxisAfterInteraction();
        DataPlot.MouseUp += (_, e) =>
        {
            if (e.ChangedButton == MouseButton.Left || e.ChangedButton == MouseButton.Middle)
                RefreshAxisAfterInteraction();
        };
    }

    private void ConfigureXAxisTicks()
    {
        var tickGen = new NumericAutomatic();
        tickGen.LabelFormatter = ChartPlotHelper.FormatIndexTick;
        DataPlot.Plot.Axes.Bottom.TickGenerator = tickGen;
    }

    /// <summary>X/Y 自动缩放；X 从 0 起，Y 从 0 起且上限不超过 65535。</summary>
    private void ApplyAxisLimits(bool hasData)
    {
        var plot = DataPlot.Plot;
        var xMax = 100.0;
        var yMax = 1000.0;

        if (hasData)
        {
            plot.Axes.AutoScale();
            var limits = plot.Axes.GetLimits();
            xMax = Math.Max(limits.Right, 1);
            yMax = Math.Max(limits.Top, 1);
            xMax = Math.Min(xMax * 1.02, ChartViewModel.MaxPoints);
            yMax = Math.Min(yMax * 1.05, ChartPlotHelper.YAxisMax);
        }

        plot.Axes.SetLimits(left: 0, right: xMax, bottom: 0, top: yMax);
    }

    private void ClampAxisLimitsToBounds()
    {
        var axes = DataPlot.Plot.Axes;
        if (axes.Bottom.Min < 0)
            axes.Bottom.Min = 0;
        if (axes.Bottom.Max > ChartViewModel.MaxPoints)
            axes.Bottom.Max = ChartViewModel.MaxPoints;
        if (axes.Left.Min < 0)
            axes.Left.Min = 0;
        if (axes.Left.Max > ChartPlotHelper.YAxisMax)
            axes.Left.Max = ChartPlotHelper.YAxisMax;
    }

    private void ApplyDefaultAxisLimits() =>
        ApplyAxisLimits(hasData: false);

    private void RefreshPlot()
    {
        var (x, ys, channelCount) = _viewModel.Chart.GetPlotData();
        var names = _viewModel.Chart.GetChannelNames();
        var active = channelCount > 0 ? channelCount : _viewModel.Chart.ActiveChannelCount;
        if (active == 0)
            active = 1;

        DataPlot.Plot.Clear();

        if (x.Length > 0)
        {
            for (var i = 0; i < Math.Min(active, DataChannels.MaxCount); i++)
            {
                if (ys[i].All(double.IsNaN))
                    continue;

                var scatter = DataPlot.Plot.Add.Scatter(x, ys[i]);
                scatter.LineWidth = 1.5f;
                scatter.MarkerSize = 0;
                scatter.Color = ChannelColors[i];
                scatter.LegendText = names[i];
            }

            DataPlot.Plot.ShowLegend();
            ScottPlotFontConfigurator.ApplyToLegend(DataPlot.Plot);
        }

        ApplyAxisLimits(x.Length > 0);

        HideScottPlotAxisTitles();
        ApplyWpfChartLabels();
        ConfigureXAxisTicks();
        ScottPlotFontConfigurator.ApplyToPlot(DataPlot.Plot);
        DataPlot.Refresh();
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
