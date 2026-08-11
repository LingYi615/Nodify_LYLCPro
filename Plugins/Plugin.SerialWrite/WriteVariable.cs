using System.ComponentModel;
using PluginBase.Models;

namespace SerialWritePlugin
{
    /// <summary>
    /// 写入变量定义
    /// 每个变量对应一个输入连接器，可配置名称、数据类型、写入地址和字符串长度
    /// </summary>
    public class WriteVariable : INotifyPropertyChanged
    {
        private string _name = "变量";
        private DataType _dataType = DataType.String;
        private string _address = "0";
        private object? _value;

        /// <summary>变量名称</summary>
        public string Name
        {
            get => _name;
            set { _name = value; OnPropertyChanged(nameof(Name)); }
        }

        /// <summary>数据类型</summary>
        public DataType DataType
        {
            get => _dataType;
            set
            {
                _dataType = value;
                OnPropertyChanged(nameof(DataType));
            }
        }

        /// <summary>写入地址（寄存器地址/偏移量）</summary>
        public string Address
        {
            get => _address;
            set { _address = value; OnPropertyChanged(nameof(Address)); }
        }


        /// <summary>写入的值（从输入连接器接收）</summary>
        public object? Value
        {
            get => _value;
            set { _value = value; OnPropertyChanged(nameof(Value)); }
        }


        public override string ToString() => $"{Name} ({DataType})";

        public event PropertyChangedEventHandler? PropertyChanged;
        protected void OnPropertyChanged(string propertyName) =>
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}