using System;
using System.Globalization;
using Avalonia.Data.Converters;

namespace dstools.Converters
{
    public class OllamaNotInstalledConverter : IValueConverter
    {
        public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            if (value is bool isNotInstalled)
            {
                // 如果传入的是安装状态，则取反
                // true表示未安装，false表示已安装
                return isNotInstalled;
            }

            return false;
        }

        public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}