using PluginBase.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media;

namespace LYCorePro.Common.Helper
{
    /// <summary>
    /// DataType 颜色映射共享工具类
    /// 按类型分组用色，避免 Converter 中的重复定义
    /// </summary>
    public static class DataTypeColorHelper
    {
        // 颜色分组（语义分组，同组共享颜色）
        private static readonly Color BoolColor = Color.FromRgb(255, 152, 0);
        private static readonly Color Int16Color = Color.FromRgb(33, 150, 243);
        private static readonly Color Int32Color = Color.FromRgb(0, 188, 212);
        private static readonly Color Int64Color = Color.FromRgb(63, 81, 181);
        private static readonly Color FloatColor = Color.FromRgb(76, 175, 80);
        private static readonly Color StringColor = Color.FromRgb(156, 39, 176);
        private static readonly Color JsonColor = Color.FromRgb(233, 30, 99);
        private static readonly Color TcpColor = Color.FromRgb(121, 85, 72);
        private static readonly Color AnyColor = Color.FromRgb(158, 158, 158);

        /// <summary>获取 DataType 对应的基础颜色</summary>
        public static Color GetColor(DataType type) => type switch
        {
            DataType.Bool => BoolColor,
            DataType.Int16 or DataType.UInt16 => Int16Color,
            DataType.Int32 or DataType.UInt32 => Int32Color,
            DataType.Int64 or DataType.UInt64 => Int64Color,
            DataType.Float or DataType.Double => FloatColor,
            DataType.String => StringColor,
            DataType.Json => JsonColor,
            DataType.TCPAgreement => TcpColor,
            DataType.Any => AnyColor,
            _ => AnyColor
        };

        /// <summary>获取背景色（基础色，Alpha 100%）</summary>
        public static SolidColorBrush GetBackgroundBrush(DataType type) =>
            new SolidColorBrush(GetColor(type));

        /// <summary>获取边框色（基础色加深）</summary>
        public static SolidColorBrush GetBorderBrush(DataType type)
        {
            var c = GetColor(type);
            return new SolidColorBrush(Color.FromRgb(
                (byte)(c.R * 0.65), (byte)(c.G * 0.65), (byte)(c.B * 0.65)));
        }

        /// <summary>获取已连接状态色（基础色提亮）</summary>
        public static SolidColorBrush GetConnectedBrush(DataType type)
        {
            var c = GetColor(type);
            return new SolidColorBrush(Color.FromRgb(
                (byte)System.Math.Min(c.R + 60, 255),
                (byte)System.Math.Min(c.G + 60, 255),
                (byte)System.Math.Min(c.B + 60, 255)));
        }

        /// <summary>获取连线颜色（与背景色相同）</summary>
        public static SolidColorBrush GetConnectionBrush(DataType type) =>
            GetBackgroundBrush(type);
    }
}
