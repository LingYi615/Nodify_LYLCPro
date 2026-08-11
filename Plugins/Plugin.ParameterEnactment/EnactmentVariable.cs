using System.ComponentModel;
using PluginBase.Models;

namespace ParameterEnactmentPlugin
{
    /// <summary>
    /// 参数设定模型定义
    /// 每个变量对应一个输出连接器，可配置名称、数据类型和值
    /// </summary>
    public class EnactmentVariable : INotifyPropertyChanged
    {
        private string _name = "参数";
        private DataType _dataType = DataType.String;
        private object? _parameterValue;
        private object? _value;

        /// <summary>变量名称</summary>
        public string Name
        {
            get => _name;
            set { _name = value; OnPropertyChanged(nameof(Name)); }
        }

        /// <summary>输出数据类型</summary>
        public DataType DataType
        {
            get => _dataType;
            set
            {
                _dataType = value;
                OnPropertyChanged(nameof(DataType));
            }
        }

        /// <summary>设定值</summary>
        public object? ParameterValue
        {
            get => _parameterValue;
            set { _parameterValue = value; OnPropertyChanged(nameof(ParameterValue)); }
        }
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