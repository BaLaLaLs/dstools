using System;
using System.Globalization;
using Avalonia.Data.Converters;

namespace dstools.Converters
{
    public class OllamaStatusConverter : IValueConverter
    {
        public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            if (value is bool status)
            {
                return status ? "已安装" : "未安装";
            }
            
            return "未知";
        }

        public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}