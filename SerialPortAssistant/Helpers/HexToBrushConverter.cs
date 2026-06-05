using System.Globalization;
using System.Windows.Data;
using System.Windows.Media;

namespace SerialPortAssistant.Helpers;

public sealed class HexToBrushConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is not string hex || string.IsNullOrWhiteSpace(hex))
            return Brushes.Black;

        try
        {
            if (!hex.StartsWith('#'))
                hex = "#" + hex;

            var color = (Color)ColorConverter.ConvertFromString(hex)!;
            return new SolidColorBrush(color);
        }
        catch
        {
            return Brushes.Black;
        }
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) =>
        throw new NotSupportedException();
}
