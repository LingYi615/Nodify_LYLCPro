using Nodify;
using PluginBase.Models;

namespace LYCorePro.Core
{
    /// <summary>
    /// 连接视图模型
    /// 表示两个连接器之间的一条连线
    /// </summary>
    public class ConnectionViewModel : ObservableObject
    {
        private ConnectorViewModel _input = default!;
        private ConnectorViewModel _output = default!;
        private DataType _dataType;
        private bool _isSelected;
        private int _order = 1;

        /// <summary>目标连接器（输入端）</summary>
        public ConnectorViewModel Input
        {
            get => _input;
            set => SetProperty(ref _input, value);
        }

        /// <summary>源连接器（输出端）</summary>
        public ConnectorViewModel Output
        {
            get => _output;
            set => SetProperty(ref _output, value);
        }

        /// <summary>连线数据类型</summary>
        public DataType DataType
        {
            get => _dataType;
            set => SetProperty(ref _dataType, value);
        }

        /// <summary>是否选中</summary>
        public bool IsSelected
        {
            get => _isSelected;
            set => SetProperty(ref _isSelected, value);
        }

        /// <summary>
        /// 执行顺序（同一输出连接多个输入时，决定目标节点的执行先后）
        /// 值越小越先执行，默认为 0，连线创建时自动分配递增序号
        /// </summary>
        public int Order
        {
            get => _order;
            set => SetProperty(ref _order, value);
        }

        public ConnectionViewModel(ConnectorViewModel input, ConnectorViewModel output)
        {
            Input = input;
            Output = output;
            DataType = input.DataType != DataType.Any ? input.DataType : output.DataType;
        }
    }
}