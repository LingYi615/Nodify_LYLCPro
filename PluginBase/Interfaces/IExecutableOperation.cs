using System.Diagnostics;

namespace PluginBase.Interfaces
{
    /// <summary>
    /// 可执行操作接口
    /// 插件的 Operation 类实现此接口后，点击节点的执行按钮时将调用 Execute() 方法
    /// 不实现此接口的节点执行时仅输出 Debug 日志
    /// </summary>
    public interface IExecutableOperation
    {
        /// <summary>
        /// 执行操作，返回是否成功
        /// </summary>
        bool Execute();

        /// <summary>
        /// 执行后的状态消息（成功/失败原因），将显示在节点 Banner 上
        /// </summary>
        string? StatusMessage { get; }

        NodeRunStatus RunStatus { get; }

    }

    /// <summary>
    /// 节点运行状态
    /// </summary>
    public enum NodeRunStatus
    {
        Disabled,
        Error,
        Running,
        Completed
    }
}