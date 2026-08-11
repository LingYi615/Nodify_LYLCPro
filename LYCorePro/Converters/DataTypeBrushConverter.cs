using System;
using System.Globalization;
using System.Windows.Data;
using System.Windows.Media;
using LYCorePro.Common.Helper;
using PluginBase.Models;

namespace LYCorePro.Converters
{
    /// <summary>
    /// DataType → SolidColorBrush 转换器
    /// 支持 Background / Border / Connected / Connection 四种模式
    /// </summary>
    public class DataTypeBrushConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is not DataType dataType)
                return new SolidColorBrush(Colors.Gray);

            var mode = parameter as string ?? "Background";

            return mode switch
            {
                "Background" => DataTypeColorHelper.GetBackgroundBrush(dataType),
                "Border" => DataTypeColorHelper.GetBorderBrush(dataType),
                "Connected" => DataTypeColorHelper.GetConnectedBrush(dataType),
                "Connection" => DataTypeColorHelper.GetConnectionBrush(dataType),
                _ => new SolidColorBrush(Colors.Gray)
            };
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}