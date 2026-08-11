using System;
using System.Collections.Generic;
using System.Windows;
using Nodify;
using PluginBase.Models;

namespace LYCorePro.Core
{
    /// <summary>
    /// 连接器视图模型
    /// </summary>
    public class ConnectorViewModel : ObservableObject
    {
        private string _id;
        private string _title;
        private DataType _dataType;
        private object? _value;
        private Point _anchor;
        private bool _isConnected;
        private bool _isProtect;
        private ConnectorDirection _direction;

        public string Id
        {
            get => _id;
            set => SetProperty(ref _id, value);
        }

        public string Title
        {
            get => _title;
            set => SetProperty(ref _title, value);
        }

        public DataType DataType
        {
            get => _dataType;
            set => SetProperty(ref _dataType, value);
        }

        public object? Value
        {
            get => _value;
            set => SetProperty(ref _value, value);
        }

        public Point Anchor
        {
            get => _anchor;
            set => SetProperty(ref _anchor, value);
        }

        public bool IsConnected
        {
            get => _isConnected;
            set => SetProperty(ref _isConnected, value);
        }

        public ConnectorDirection Direction
        {
            get => _direction;
            set => SetProperty(ref _direction, value);
        }

        public bool IsProtect
        {
            get => _isProtect;
            set => SetProperty(ref _isProtect, value);
        }

        // 值观察者列表（用于值传播）
        public List<ConnectorViewModel> ValueObservers { get; } = new List<ConnectorViewModel>();

        public ConnectorViewModel()
        {
            _id = Guid.NewGuid().ToString();
            _title = string.Empty;
            _dataType = DataType.Any;
            _anchor = new Point(0, 0);
            _isConnected = false;
            _isProtect = false;
            _direction = ConnectorDirection.Input;
        }
    }
}