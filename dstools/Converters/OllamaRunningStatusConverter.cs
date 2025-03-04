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
                // 如果状态是"已停止"，则返回 true 表示"启动"按钮可用
                // 如果状态是"运行中"，则返回 false 表示"启动"按钮不可用
                return status == RunningStatus.Stopped;
            }
            
            // 默认返回 false
            return false;
        }

        public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
} 