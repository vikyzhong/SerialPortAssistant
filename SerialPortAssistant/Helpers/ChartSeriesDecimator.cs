namespace SerialPortAssistant.Helpers;

/// <summary>将大量采样点抽稀到适合屏幕绘制的数量（保留折线形状）。</summary>
public static class ChartSeriesDecimator
{
    public const int MinDisplayPoints = 256;
    public const int MaxDisplayPoints = 4000;

    public static int TargetPointCount(double plotWidthPixels) =>
        (int)Math.Clamp(plotWidthPixels * 2, MinDisplayPoints, MaxDisplayPoints);

    public static (double[] X, double[] Y) Decimate(double[] x, double[] y, int maxPoints)
    {
        if (x.Length == 0 || y.Length == 0 || x.Length != y.Length)
            return ([], []);

        if (x.Length <= maxPoints)
            return (x, y);

        var bucketCount = maxPoints / 2;
        if (bucketCount < 1)
            bucketCount = 1;

        var bucketSize = (double)x.Length / bucketCount;
        var outX = new List<double>(maxPoints);
        var outY = new List<double>(maxPoints);

        for (var b = 0; b < bucketCount; b++)
        {
            var start = (int)(b * bucketSize);
            var end = (int)Math.Min(x.Length, (b + 1) * bucketSize);
            if (start >= end)
                continue;

            var minIdx = start;
            var maxIdx = start;
            var minY = y[start];
            var maxY = y[start];

            for (var i = start + 1; i < end; i++)
            {
                if (double.IsNaN(y[i]))
                    continue;

                if (y[i] < minY)
                {
                    minY = y[i];
                    minIdx = i;
                }

                if (y[i] > maxY)
                {
                    maxY = y[i];
                    maxIdx = i;
                }
            }

            if (minIdx <= maxIdx)
            {
                outX.Add(x[minIdx]);
                outY.Add(y[minIdx]);
                if (maxIdx != minIdx)
                {
                    outX.Add(x[maxIdx]);
                    outY.Add(y[maxIdx]);
                }
            }
            else
            {
                outX.Add(x[maxIdx]);
                outY.Add(y[maxIdx]);
                outX.Add(x[minIdx]);
                outY.Add(y[minIdx]);
            }
        }

        return (outX.ToArray(), outY.ToArray());
    }
}
