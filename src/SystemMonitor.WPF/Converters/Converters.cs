using System.Globalization;
using System.Windows;
using System.Windows.Data;
using System.Windows.Media;

namespace SystemMonitor.WPF.Converters;

/// <summary>
/// Returns a colour brush based on a percentage value.
/// Green below 60%, amber 60–80%, red above 80%.
/// </summary>
[ValueConversion(typeof(double), typeof(Brush))]
public sealed class PercentToColorConverter : IValueConverter
{
    public static readonly PercentToColorConverter Instance = new();

    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is not double pct) return Brushes.Gray;
        return pct switch
        {
            >= 80 => new SolidColorBrush(Color.FromRgb(0xF4, 0x43, 0x36)),  // red
            >= 60 => new SolidColorBrush(Color.FromRgb(0xFF, 0x98, 0x00)),  // amber
            _     => new SolidColorBrush(Color.FromRgb(0x4C, 0xAF, 0x50)),  // green
        };
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        => DependencyProperty.UnsetValue;
}

/// <summary>Converts a boolean to Visibility (true → Visible, false → Collapsed).</summary>
[ValueConversion(typeof(bool), typeof(Visibility))]
public sealed class BoolToVisibilityConverter : IValueConverter
{
    public static readonly BoolToVisibilityConverter Instance = new();

    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        => value is true ? Visibility.Visible : Visibility.Collapsed;

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        => value is Visibility.Visible;
}

/// <summary>Converts a boolean to a "Start" / "Stop" string for the toggle button.</summary>
[ValueConversion(typeof(bool), typeof(string))]
public sealed class MonitoringStateConverter : IValueConverter
{
    public static readonly MonitoringStateConverter Instance = new();

    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        => value is true ? "⏹ Stop" : "▶ Start";

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        => DependencyProperty.UnsetValue;
}
