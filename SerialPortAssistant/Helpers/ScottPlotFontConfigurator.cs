using System.Windows.Media;

namespace SerialPortAssistant.Helpers;

/// <summary>
/// 为 ScottPlot 图例等选择本机可用的中文字体。
/// 图表标题与坐标轴中文由 WPF 文本绘制，不依赖 ScottPlot 字体。
/// </summary>
public static class ScottPlotFontConfigurator
{
    private static readonly string[] FontCandidates =
    [
        "Microsoft YaHei UI",
        "Microsoft YaHei",
        "微软雅黑",
        "SimHei",
        "黑体",
        "PingFang SC",
        "Noto Sans CJK SC"
    ];

    public static string ChineseFontName { get; private set; } = "Microsoft YaHei UI";

    public static void Initialize()
    {
        ChineseFontName = ResolveInstalledFont();
        ScottPlot.Fonts.Default = ChineseFontName;
    }

    public static void ApplyToPlot(ScottPlot.Plot plot)
    {
        try
        {
            plot.Font.Set(ChineseFontName);
        }
        catch
        {
            // 部分版本仅支持 Fonts.Default
        }
    }

    public static void ApplyToLegend(ScottPlot.Plot plot)
    {
        try
        {
            plot.Legend.FontName = ChineseFontName;
        }
        catch
        {
            // 忽略
        }
    }

    private static string ResolveInstalledFont()
    {
        foreach (var name in FontCandidates)
        {
            if (IsFontAvailable(name))
                return name;
        }

        return "Microsoft YaHei UI";
    }

    private static bool IsFontAvailable(string fontFamilyName)
    {
        try
        {
            var family = new FontFamily(fontFamilyName);
            return family.Source.Length > 0;
        }
        catch
        {
            return false;
        }
    }
}
