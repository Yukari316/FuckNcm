using Avalonia.Data.Converters;
using Avalonia.Media;
using System;
using System.Globalization;
using FuckNcm.Models;

namespace FuckNcm.Converters;

/// <summary>
/// 状态颜色的转换器
/// </summary>
public class ItemStatusToColorConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is ItemStatus status)
            return status switch
                   {
                       ItemStatus.UNPARSED => new SolidColorBrush(Colors.Blue),
                       ItemStatus.PARSING => new SolidColorBrush(Colors.DarkSlateBlue),
                       ItemStatus.PARSED => new SolidColorBrush(Colors.DarkGreen),
                       ItemStatus.ERROR => new SolidColorBrush(Colors.DarkRed),
                       ItemStatus.MOVED => new SolidColorBrush(Colors.DarkCyan),
                       ItemStatus.SKIPPED => new SolidColorBrush(Colors.DarkOrange),
                       _ => throw new ArgumentOutOfRangeException(nameof(value), $"ItemStatus get {(int)value}")
                   };
        throw new NotSupportedException($"ItemStatus {value} is not supported");
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) =>
        throw new NotImplementedException();
}