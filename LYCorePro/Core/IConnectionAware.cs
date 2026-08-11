using System.Collections.ObjectModel;

namespace LYCorePro.Core
{
    /// <summary>
    /// 连线感知接口
    /// 实现此接口的插件操作可以感知和管理编辑器中的连线，
    /// 用于在参数变更时主动断开相关连线
    /// </summary>
    public interface IConnectionAware
    {
        /// <summary>编辑器中所有连线的引用（由编辑器在节点创建时注入）</summary>
        ObservableCollection<ConnectionViewModel> Connections { get; set; }

        /// <summary>断开指定连接器上的所有连线</summary>
        /// <param name="connector">要断开连线的连接器</param>
        void DisconnectConnector(ConnectorViewModel connector);
    }
}