using System;
using System.Diagnostics;
using System.Globalization;
using Avalonia.Data.Converters;

namespace dstools.Converters;

public class DebugConverter : IValueConverter
{
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        Debugger.Break(); // 在此处断点
        return value;
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}