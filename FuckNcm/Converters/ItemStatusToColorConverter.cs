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
    private static readonly SolidColorBrush UnparsedBrush = new(Colors.Blue);
    private static readonly SolidColorBrush ParsingBrush  = new(Colors.DarkSlateBlue);
    private static readonly SolidColorBrush ParsedBrush   = new(Colors.DarkGreen);
    private static readonly SolidColorBrush ErrorBrush    = new(Colors.DarkRed);
    private static readonly SolidColorBrush MovedBrush    = new(Colors.DarkCyan);
    private static readonly SolidColorBrush SkippedBrush  = new(Colors.DarkOrange);

    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is ItemStatus status)
            return status switch
                   {
                       ItemStatus.UNPARSED => UnparsedBrush,
                       ItemStatus.PARSING => ParsingBrush,
                       ItemStatus.PARSED => ParsedBrush,
                       ItemStatus.ERROR => ErrorBrush,
                       ItemStatus.MOVED => MovedBrush,
                       ItemStatus.SKIPPED => SkippedBrush,
                       _ => throw new ArgumentOutOfRangeException(nameof(value), $"ItemStatus get {(int)value}")
                   };
        throw new NotSupportedException($"ItemStatus {value} is not supported");
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) =>
        throw new NotImplementedException();
}