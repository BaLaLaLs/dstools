using System;
using System.Globalization;
using Avalonia.Data.Converters;
using dstools.ViewModels;

namespace dstools.Converters
{
    public class OllamaNotInstalledConverter : IValueConverter
    {
        public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            if (value is InstallStatus status)
            {
                // 如果是已安装状态，返回true以显示相关模块
                // 如果是未安装状态，返回false以隐藏相关模块
                return status == InstallStatus.Installed;
            }

            return false;
        }

        public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}