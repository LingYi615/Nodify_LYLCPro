namespace LYCorePro.Core
{
    /// <summary>
    /// 插件实现此接口以提供右侧参数面板的标题
    /// 不实现此接口的插件将使用默认标题 "{节点名} 参数设定"
    /// </summary>
    public interface ISettingsPanelProvider
    {
        /// <summary>
        /// 参数面板标题
        /// </summary>
        string SettingsPanelTitle { get; }
    }
}