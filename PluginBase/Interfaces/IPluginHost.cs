using System;
using System.Collections.Generic;

namespace PluginBase.Interfaces
{
    /// <summary>
    /// 插件宿主接口
    /// </summary>
    public interface IPluginHost
    {
        void Log(string message, LogLevel level = LogLevel.Info);
        void RegisterOperation(Type operationType, IPlugin plugin);
        IEnumerable<IPlugin> GetPlugins();
        T? GetConfigValue<T>(string key, T? defaultValue = default);
        void SetConfigValue<T>(string key, T value);
    }

    public enum LogLevel
    {
        Debug,
        Info,
        Warning,
        Error
    }
}