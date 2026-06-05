namespace SerialPortAssistant.Helpers;

public static class ChartPlotHelper
{
    public const string PlotTitle = "数据显示（UINT16）";
    public const string YAxisLabel = "Y轴（0-65535）";
    public const string XAxisLabel = "X轴";
    public const double YAxisMax = 65535;

    public static string ChineseFontName => ScottPlotFontConfigurator.ChineseFontName;

    public static string FormatIndexTick(double index) =>
        Math.Max(0, (int)Math.Round(index)).ToString();
}
