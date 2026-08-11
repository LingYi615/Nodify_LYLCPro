using System;
using System.Windows;

namespace PluginBase.Interfaces
{
    /// <summary>
    /// 插件接口
    /// </summary>
    public interface IPlugin
    {
        string Name { get; }
        Version Version { get; }
        string Description { get; }
        string Author { get; }
        Type[] OperationTypes { get; }

        void Initialize(IPluginHost host);
        void Cleanup();

        DataTemplate? GetTemplate(Type operationType);
        object? CreateInstance(Type operationType);
        IPluginMetadata GetMetadata();
    }
}