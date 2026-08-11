using System;
using System.Windows;

namespace PluginBase.Models
{
    /// <summary>
    /// 连接器方向
    /// </summary>
    public enum ConnectorDirection
    {
        Input,
        Output
    }

    /// <summary>
    /// 连接器模型
    /// </summary>
    public class ConnectorModel
    {
        public string Id { get; set; } = Guid.NewGuid().ToString();
        public string Title { get; set; } = string.Empty;
        public DataType DataType { get; set; } = DataType.Any;
        public object? Value { get; set; }
        public Point Anchor { get; set; }
        public bool IsConnected { get; set; }
        public ConnectorDirection Direction { get; set; } = ConnectorDirection.Input;
    }
}