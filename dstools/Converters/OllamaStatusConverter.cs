using System;
using System.Globalization;
using Avalonia.Data.Converters;
using dstools.Models;
using dstools.ViewModels;

namespace dstools.Converters
{
    public class OllamaStatusConverter : IValueConverter
    {
        public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            if (value is InstallStatus installStatus)
            {
                return installStatus switch
                {
                    InstallStatus.NotInstalled => "未安装",
                    InstallStatus.Installed => "已安装",
                    _ => "未知"
                };
            }
            
            if (value is RunningStatus runningStatus)
            {
                return runningStatus switch
                {
                    RunningStatus.NotRunning => "未运行",
                    RunningStatus.Running => "运行中",
                    RunningStatus.Stopped => "未运行",
                    _ => "未知"
                };
            }
            
            return "未知";
        }

        public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}