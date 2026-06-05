using System.Globalization;
using System.Text;
using SerialPortAssistant.Models;

namespace SerialPortAssistant.Helpers;

public static class ChartCsvExporter
{
    public static string BuildCsv(
        IReadOnlyList<double> xValues,
        IReadOnlyList<double>[] yValues,
        int activeChannelCount,
        string[] channelNames)
    {
        var count = xValues.Count;
        if (count == 0)
            throw new InvalidOperationException("当前没有可导出的曲线数据。");

        var channels = Math.Clamp(activeChannelCount, 1, DataChannels.MaxCount);
        var names = channelNames.Length >= channels
            ? channelNames
            : DataChannelNames.CreateAccelerometerDefaults().ToArray();

        var sb = new StringBuilder(count * (channels + 1) * 12);
        sb.Append("Index");
        for (var i = 0; i < channels; i++)
            sb.Append(',').Append(EscapeCsvField(names[i]));

        sb.AppendLine();

        var inv = CultureInfo.InvariantCulture;
        for (var row = 0; row < count; row++)
        {
            sb.Append(xValues[row].ToString("0", inv));
            for (var ch = 0; ch < channels; ch++)
            {
                var y = yValues[ch][row];
                sb.Append(',');
                if (double.IsNaN(y))
                    sb.Append("");
                else
                    sb.Append(((ushort)Math.Clamp(y, 0, 65535)).ToString(inv));
            }

            sb.AppendLine();
        }

        return sb.ToString();
    }

    private static string EscapeCsvField(string field)
    {
        if (field.Contains(',') || field.Contains('"') || field.Contains('\n'))
            return '"' + field.Replace("\"", "\"\"") + '"';
        return field;
    }
}
