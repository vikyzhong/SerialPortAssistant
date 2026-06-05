using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Shapes;
using SerialPortAssistant.Helpers;
using SerialPortAssistant.Models;
using SerialPortAssistant.ViewModels;

namespace SerialPortAssistant.Views;

public partial class LightweightChartControl : UserControl
{
    private const double LeftMargin = 4;
    private const double RightMargin = 8;
    private const double TopMargin = 8;
    private const double BottomMargin = 4;

    private readonly List<Polyline> _seriesLines = [];
    private readonly Line[] _gridLines = new Line[12];

    private double[] _x = [];
    private double[][] _ys = [];
    private int _activeChannels;
    private string[] _channelNames = ["X", "Y", "Z"];

    private double _xMin;
    private double _xMax = 100;
    private double _yMin;
    private double _yMax = 1000;

    private bool _isPanning;
    private Point _panStart;
    private double _panStartXMin;
    private double _panStartXMax;
    private double _panStartYMin;
    private double _panStartYMax;

    public LightweightChartControl()
    {
        InitializeComponent();
        Loaded += (_, _) => RenderChart();
        SizeChanged += (_, _) => RenderChart();

        for (var i = 0; i < _gridLines.Length; i++)
        {
            var line = new Line
            {
                Stroke = new SolidColorBrush(Color.FromRgb(224, 224, 224)),
                StrokeThickness = 1,
                IsHitTestVisible = false
            };
            _gridLines[i] = line;
            PlotCanvas.Children.Add(line);
        }

        for (var i = 0; i < DataChannels.MaxCount; i++)
        {
            var polyline = new Polyline
            {
                StrokeThickness = 1.5,
                Stroke = ChartPlotHelper.GetChannelBrush(i),
                Fill = Brushes.Transparent,
                StrokeLineJoin = PenLineJoin.Round,
                IsHitTestVisible = false
            };
            _seriesLines.Add(polyline);
            PlotCanvas.Children.Add(polyline);
        }
    }

    public void UpdatePlot(double[] x, double[][] ys, int activeChannelCount, string[] channelNames)
    {
        _x = x;
        _ys = ys;
        _activeChannels = activeChannelCount > 0
            ? Math.Min(activeChannelCount, DataChannels.MaxCount)
            : Math.Min(Math.Max(1, activeChannelCount), DataChannels.MaxCount);
        if (_activeChannels == 0)
            _activeChannels = 1;

        _channelNames = channelNames.Length >= DataChannels.MaxCount
            ? channelNames
            : ["X", "Y", "Z"];

        ApplyAutoScale(x.Length > 0);
        RenderChart();
    }

    private void ApplyAutoScale(bool hasData)
    {
        if (!hasData)
        {
            _xMin = 0;
            _xMax = 100;
            _yMin = 0;
            _yMax = 1000;
            return;
        }

        var xMax = 1.0;
        var yMax = 1.0;

        for (var i = 0; i < Math.Min(_activeChannels, DataChannels.MaxCount); i++)
        {
            if (i >= _ys.Length)
                break;

            foreach (var y in _ys[i])
            {
                if (double.IsNaN(y))
                    continue;
                yMax = Math.Max(yMax, y);
            }
        }

        if (_x.Length > 0)
            xMax = Math.Max(_x[^1], 1);

        _xMin = 0;
        _xMax = Math.Min(xMax * 1.02, ChartViewModel.MaxPoints);
        _yMin = 0;
        _yMax = Math.Min(Math.Max(yMax * 1.05, 1), ChartPlotHelper.YAxisMax);
    }

    private void RenderChart()
    {
        if (!IsLoaded || PlotCanvas.ActualWidth < 1 || PlotCanvas.ActualHeight < 1)
            return;

        var plotW = PlotCanvas.ActualWidth;
        var plotH = PlotCanvas.ActualHeight;
        var targetPoints = ChartSeriesDecimator.TargetPointCount(plotW);

        for (var i = 0; i < DataChannels.MaxCount; i++)
        {
            var line = _seriesLines[i];
            if (i >= _activeChannels || i >= _ys.Length || _x.Length == 0 || _ys[i].All(double.IsNaN))
            {
                line.Points.Clear();
                line.Visibility = Visibility.Collapsed;
                continue;
            }

            var (dx, dy) = ChartSeriesDecimator.Decimate(_x, _ys[i], targetPoints);
            var points = new PointCollection(dx.Length);
            for (var p = 0; p < dx.Length; p++)
            {
                if (double.IsNaN(dy[p]))
                    continue;
                points.Add(DataToScreen(dx[p], dy[p], plotW, plotH));
            }

            line.Points = points;
            line.Visibility = points.Count > 0 ? Visibility.Visible : Visibility.Collapsed;
        }

        DrawGrid(plotW, plotH);
        DrawTicks(plotW, plotH);
        UpdateLegend();
    }

    private void DrawGrid(double plotW, double plotH)
    {
        var xTicks = BuildTicks(_xMin, _xMax, 5);
        var yTicks = BuildTicks(_yMin, _yMax, 5);

        var idx = 0;
        foreach (var xt in xTicks)
        {
            if (idx >= _gridLines.Length)
                break;
            var x = DataToScreen(xt, _yMin, plotW, plotH).X;
            SetGridLine(_gridLines[idx++], x, 0, x, plotH);
        }

        foreach (var yt in yTicks)
        {
            if (idx >= _gridLines.Length)
                break;
            var y = DataToScreen(_xMin, yt, plotW, plotH).Y;
            SetGridLine(_gridLines[idx++], 0, y, plotW, y);
        }

        for (; idx < _gridLines.Length; idx++)
            _gridLines[idx].Visibility = Visibility.Collapsed;
    }

    private static void SetGridLine(Line line, double x1, double y1, double x2, double y2)
    {
        line.X1 = x1;
        line.Y1 = y1;
        line.X2 = x2;
        line.Y2 = y2;
        line.Visibility = Visibility.Visible;
    }

    private void DrawTicks(double plotW, double plotH)
    {
        YTickCanvas.Children.Clear();
        XTickCanvas.Children.Clear();

        var xTicks = BuildTicks(_xMin, _xMax, 5);
        var yTicks = BuildTicks(_yMin, _yMax, 5);

        foreach (var yt in yTicks)
        {
            var y = DataToScreen(_xMin, yt, plotW, plotH).Y;
            var tb = new TextBlock
            {
                Text = FormatTick(yt),
                FontSize = 11,
                Foreground = Brushes.Gray
            };
            Canvas.SetRight(tb, 4);
            Canvas.SetTop(tb, y - 8);
            YTickCanvas.Children.Add(tb);
        }

        foreach (var xt in xTicks)
        {
            var x = DataToScreen(xt, _yMin, plotW, plotH).X;
            var tb = new TextBlock
            {
                Text = ChartPlotHelper.FormatIndexTick(xt),
                FontSize = 11,
                Foreground = Brushes.Gray
            };
            Canvas.SetLeft(tb, x - 16);
            Canvas.SetTop(tb, 2);
            XTickCanvas.Children.Add(tb);
        }
    }

    private void UpdateLegend()
    {
        LegendPanel.Children.Clear();
        for (var i = 0; i < Math.Min(_activeChannels, DataChannels.MaxCount); i++)
        {
            if (i >= _ys.Length || _x.Length == 0 || _ys[i].All(double.IsNaN))
                continue;

            var row = new StackPanel { Orientation = Orientation.Horizontal, Margin = new Thickness(6, 0, 0, 0) };
            row.Children.Add(new Border
            {
                Width = 14,
                Height = 3,
                Background = ChartPlotHelper.GetChannelBrush(i),
                VerticalAlignment = VerticalAlignment.Center,
                Margin = new Thickness(0, 0, 4, 0)
            });
            row.Children.Add(new TextBlock
            {
                Text = i < _channelNames.Length ? _channelNames[i] : $"CH{i + 1}",
                FontSize = 11,
                Foreground = Brushes.DimGray
            });
            LegendPanel.Children.Add(row);
        }
    }

    private Point DataToScreen(double x, double y, double plotW, double plotH)
    {
        var innerW = Math.Max(1, plotW - LeftMargin - RightMargin);
        var innerH = Math.Max(1, plotH - TopMargin - BottomMargin);
        var nx = (_xMax - _xMin) > 0 ? (x - _xMin) / (_xMax - _xMin) : 0;
        var ny = (_yMax - _yMin) > 0 ? (y - _yMin) / (_yMax - _yMin) : 0;
        return new Point(LeftMargin + nx * innerW, TopMargin + (1 - ny) * innerH);
    }

    private static string FormatTick(double value) =>
        Math.Abs(value) >= 1000 ? $"{value:F0}" : value.ToString("G4");

    private static IReadOnlyList<double> BuildTicks(double min, double max, int count)
    {
        if (max <= min)
            return [min];

        var step = NiceStep((max - min) / Math.Max(1, count - 1));
        var start = Math.Floor(min / step) * step;
        var ticks = new List<double>();
        for (var v = start; v <= max + step * 0.5; v += step)
        {
            if (v >= min - step * 0.01)
                ticks.Add(v);
        }

        if (ticks.Count == 0)
            ticks.Add(min);

        return ticks;
    }

    private static double NiceStep(double rough)
    {
        if (rough <= 0)
            return 1;
        var exp = Math.Floor(Math.Log10(rough));
        var basePow = Math.Pow(10, exp);
        var frac = rough / basePow;
        var nice = frac switch
        {
            < 1.5 => 1,
            < 3 => 2,
            < 7 => 5,
            _ => 10
        };
        return nice * basePow;
    }

    private void ClampView()
    {
        if (_xMin < 0)
        {
            var shift = -_xMin;
            _xMin += shift;
            _xMax += shift;
        }

        if (_xMax > ChartViewModel.MaxPoints)
        {
            var shift = _xMax - ChartViewModel.MaxPoints;
            _xMin -= shift;
            _xMax -= shift;
        }

        if (_xMin < 0)
            _xMin = 0;

        if (_yMin < 0)
        {
            var shift = -_yMin;
            _yMin += shift;
            _yMax += shift;
        }

        if (_yMax > ChartPlotHelper.YAxisMax)
        {
            var shift = _yMax - ChartPlotHelper.YAxisMax;
            _yMin -= shift;
            _yMax -= shift;
        }

        if (_yMin < 0)
            _yMin = 0;

        if (_xMax <= _xMin)
            _xMax = _xMin + 1;
        if (_yMax <= _yMin)
            _yMax = _yMin + 1;
    }

    private void PlotCanvas_MouseWheel(object sender, MouseWheelEventArgs e)
    {
        var pos = e.GetPosition(PlotCanvas);
        var plotW = PlotCanvas.ActualWidth;
        var plotH = PlotCanvas.ActualHeight;
        if (plotW < 1 || plotH < 1)
            return;

        var data = ScreenToData(pos, plotW, plotH);
        var factor = e.Delta > 0 ? 0.85 : 1.15;

        _xMin = data.X - (data.X - _xMin) * factor;
        _xMax = data.X + (_xMax - data.X) * factor;
        _yMin = data.Y - (data.Y - _yMin) * factor;
        _yMax = data.Y + (_yMax - data.Y) * factor;

        ClampView();
        RenderChart();
        e.Handled = true;
    }

    private Point ScreenToData(Point screen, double plotW, double plotH)
    {
        var innerW = Math.Max(1, plotW - LeftMargin - RightMargin);
        var innerH = Math.Max(1, plotH - TopMargin - BottomMargin);
        var nx = (screen.X - LeftMargin) / innerW;
        var ny = 1 - (screen.Y - TopMargin) / innerH;
        return new Point(_xMin + nx * (_xMax - _xMin), _yMin + ny * (_yMax - _yMin));
    }

    private void PlotCanvas_PreviewMouseDown(object sender, MouseButtonEventArgs e)
    {
        if (e.ChangedButton is not (MouseButton.Left or MouseButton.Middle))
            return;

        _isPanning = true;
        _panStart = e.GetPosition(PlotCanvas);
        _panStartXMin = _xMin;
        _panStartXMax = _xMax;
        _panStartYMin = _yMin;
        _panStartYMax = _yMax;
        PlotCanvas.CaptureMouse();
        e.Handled = true;
    }

    private void PlotCanvas_MouseMove(object sender, MouseEventArgs e)
    {
        if (!_isPanning)
            return;

        var pos = e.GetPosition(PlotCanvas);
        var plotW = PlotCanvas.ActualWidth;
        var plotH = PlotCanvas.ActualHeight;
        if (plotW < 1 || plotH < 1)
            return;

        var dx = (pos.X - _panStart.X) / plotW * (_xMax - _xMin);
        var dy = (pos.Y - _panStart.Y) / plotH * (_yMax - _yMin);

        _xMin = _panStartXMin - dx;
        _xMax = _panStartXMax - dx;
        _yMin = _panStartYMin + dy;
        _yMax = _panStartYMax + dy;

        ClampView();
        RenderChart();
    }

    private void PlotCanvas_PreviewMouseUp(object sender, MouseButtonEventArgs e)
    {
        if (!_isPanning)
            return;

        _isPanning = false;
        PlotCanvas.ReleaseMouseCapture();
    }

    private void PlotCanvas_MouseLeave(object sender, MouseEventArgs e)
    {
        if (!_isPanning)
            return;

        _isPanning = false;
        PlotCanvas.ReleaseMouseCapture();
    }
}
