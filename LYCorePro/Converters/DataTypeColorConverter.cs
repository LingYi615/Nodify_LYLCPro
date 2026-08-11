using System;
using System.Globalization;
using System.Windows.Data;
using System.Windows.Media;
using LYCorePro.Common.Helper;
using PluginBase.Models;

namespace LYCorePro.Converters
{
    /// <summary>
    /// DataType → Color 转换器
    /// </summary>
    public class DataTypeColorConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is not DataType dataType)
                return Colors.Gray;

            return DataTypeColorHelper.GetColor(dataType);
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}