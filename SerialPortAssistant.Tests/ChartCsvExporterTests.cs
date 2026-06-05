using SerialPortAssistant.Helpers;
using Xunit;

namespace SerialPortAssistant.Tests;

public sealed class ChartCsvExporterTests
{
    [Fact]
    public void BuildCsv_WritesHeaderAndRows()
    {
        var x = new List<double> { 0, 1 };
        var y0 = new List<double> { 100, 200 };
        var y1 = new List<double> { 300, 400 };
        var y = new[] { y0, y1, new List<double>() };

        var csv = ChartCsvExporter.BuildCsv(x, y, activeChannelCount: 2, ["X", "Y"]);

        Assert.Contains("Index,X,Y", csv);
        Assert.Contains("0,100,300", csv);
        Assert.Contains("1,200,400", csv);
    }

    [Fact]
    public void BuildCsv_Empty_Throws()
    {
        Assert.Throws<InvalidOperationException>(() =>
            ChartCsvExporter.BuildCsv([], [new List<double>(), new List<double>(), new List<double>()], 1,
                ["X", "Y", "Z"]));
    }
}
