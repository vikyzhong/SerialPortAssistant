using System.Windows.Media;

namespace SerialPortAssistant.Helpers;

public static class ChartPlotHelper
{
    public const string PlotTitle = "数据显示（UINT16）";
    public const string YAxisLabel = "Y轴（0-65535）";
    public const string XAxisLabel = "X轴";
    public const double YAxisMax = 65535;

    private static readonly Color[] ChannelColorValues =
    [
        Color.FromRgb(0x21, 0x96, 0xF3),
        Color.FromRgb(0xFF, 0x57, 0x22),
        Color.FromRgb(0x4C, 0xAF, 0x50)
    ];

    public static Brush GetChannelBrush(int index) =>
        new SolidColorBrush(ChannelColorValues[Math.Clamp(index, 0, ChannelColorValues.Length - 1)]);

    public static string FormatIndexTick(double index) =>
        Math.Max(0, (int)Math.Round(index)).ToString();
}
