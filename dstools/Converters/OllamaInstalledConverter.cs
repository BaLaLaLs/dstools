using System;
using System.Globalization;
using Avalonia.Data.Converters;
using dstools.ViewModels;

namespace dstools.Converters
{
    public class OllamaInstalledConverter : IValueConverter
    {
        public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            if (value is InstallStatus status)
            {
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