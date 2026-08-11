using System.ComponentModel;
using PluginBase.Models;

namespace SerialReadPlugin
{
    /// <summary>
    /// 读取变量定义
    /// 每个变量对应一个输出连接器，可配置名称、数据类型和读取地址
    /// </summary>
    public class ReadVariable : INotifyPropertyChanged
    {
        private string _name = "变量";
        private DataType _dataType = DataType.String;
        private string _address = "0";
        private ushort _stringLength = 256;
        private object? _value;

        public string Name
        {
            get => _name;
            set { _name = value; OnPropertyChanged(nameof(Name)); }
        }

        public DataType DataType
        {
            get => _dataType;
            set
            {
                _dataType = value;
                OnPropertyChanged(nameof(DataType));
                OnPropertyChanged(nameof(IsStringType));
            }
        }

        public string Address
        {
            get => _address;
            set { _address = value; OnPropertyChanged(nameof(Address)); }
        }

        public ushort StringLength
        {
            get => _stringLength;
            set { _stringLength = value; OnPropertyChanged(nameof(StringLength)); }
        }

        public bool IsStringType => DataType == DataType.String;

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