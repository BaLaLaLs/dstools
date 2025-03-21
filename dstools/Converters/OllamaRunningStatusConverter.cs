using System;
using System.Globalization;
using Avalonia.Data.Converters;
using dstools.ViewModels;

namespace dstools.Converters
{
    public class OllamaRunningStatusConverter : IValueConverter
    {
        public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            if (value is RunningStatus status)
            {
                return status == RunningStatus.Running;
            }
            
            return false;
        }

        public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}