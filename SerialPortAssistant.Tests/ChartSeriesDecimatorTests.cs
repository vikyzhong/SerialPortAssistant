using SerialPortAssistant.Helpers;
using Xunit;

namespace SerialPortAssistant.Tests;

public sealed class ChartSeriesDecimatorTests
{
    [Fact]
    public void Decimate_ShortSeries_ReturnsSameLength()
    {
        var x = new[] { 0.0, 1, 2, 3 };
        var y = new[] { 10.0, 20, 30, 40 };

        var (dx, dy) = ChartSeriesDecimator.Decimate(x, y, 100);

        Assert.Equal(x.Length, dx.Length);
        Assert.Equal(y, dy);
    }

    [Fact]
    public void Decimate_LongSeries_ReducesPointCount()
    {
        var x = Enumerable.Range(0, 10_000).Select(i => (double)i).ToArray();
        var y = x.Select(v => v * 0.5).ToArray();

        var (dx, dy) = ChartSeriesDecimator.Decimate(x, y, 500);

        Assert.True(dx.Length <= 500);
        Assert.Equal(dx.Length, dy.Length);
        Assert.True(dx.Length > 0);
    }
}
