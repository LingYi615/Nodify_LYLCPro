using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Windows;
using PluginBase.Interfaces;
using PluginBase.Models;
using LYCorePro.Views;

namespace LYCorePro.Core
{
    public class PluginManager : IPluginHost
    {
        private readonly NodifyEditorView _editor;
        private readonly string _pluginDirectory;
        private readonly Dictionary<string, IPlugin> _loadedPlugins = new();
        private readonly Dictionary<Type, IPlugin> _operationTypeMap = new();
        private readonly ObservableCollection<PluginMetadata> _pluginMetadata = new();

        public IReadOnlyList<IPlugin> LoadedPlugins => _loadedPlugins.Values.ToList();
        public ObservableCollection<PluginMetadata> PluginMetadata => _pluginMetadata;

        public event EventHandler<PluginLoadedEventArgs>? PluginLoaded;
        public event EventHandler<PluginUnloadedEventArgs>? PluginUnloaded;
        public event EventHandler<PluginErrorEventArgs>? PluginError;

        public PluginManager(NodifyEditorView editor, string pluginDirectory = "Plugins")
        {
            _editor = editor ?? throw new ArgumentNullException(nameof(editor));
            _pluginDirectory = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, pluginDirectory);

            if (!Directory.Exists(_pluginDirectory))
            {
                Directory.CreateDirectory(_pluginDirectory);
            }
        }

        public void LoadAllPlugins()
        {
            var dllFiles = Directory.GetFiles(_pluginDirectory, "*.dll", SearchOption.TopDirectoryOnly);

            foreach (var dllPath in dllFiles)
            {
                try
                {
                    LoadPlugin(dllPath);
                }
                catch (Exception ex)
                {
                    OnPluginError(new PluginErrorEventArgs(
                        Path.GetFileName(dllPath),
                        $"加载失败: {ex.Message}",
                        ex
                    ));
                }
            }
        }

        public void LoadPlugin(string dllPath)
        {
            if (!File.Exists(dllPath))
                throw new FileNotFoundException($"插件文件不存在: {dllPath}");

            var assembly = Assembly.LoadFrom(dllPath);
            var pluginTypes = assembly.GetTypes()
                .Where(t => typeof(IPlugin).IsAssignableFrom(t) && !t.IsInterface && !t.IsAbstract)
                .ToList();

            if (!pluginTypes.Any())
                throw new InvalidOperationException($"未找到插件实现: {Path.GetFileName(dllPath)}");

            foreach (var pluginType in pluginTypes)
            {
                try
                {
                    var plugin = (IPlugin)Activator.CreateInstance(pluginType)!;
                    RegisterPlugin(plugin, dllPath);
                }
                catch (Exception ex)
                {
                    OnPluginError(new PluginErrorEventArgs(
                        pluginType.Name,
                        $"实例化失败: {ex.Message}",
                        ex
                    ));
                }
            }
        }

        private void RegisterPlugin(IPlugin plugin, string dllPath)
        {
            plugin.Initialize(this);

            foreach (var operationType in plugin.OperationTypes)
            {
                if (!_operationTypeMap.ContainsKey(operationType))
                {
                    _operationTypeMap[operationType] = plugin;
                }

                var template = plugin.GetTemplate(operationType);
                if (template != null)
                {
                    RegisterTemplate(operationType, template);
                }
            }

            _loadedPlugins[plugin.Name] = plugin;

            var metadata = plugin.GetMetadata() as PluginMetadata;
            if (metadata != null)
            {
                _pluginMetadata.Add(metadata);
            }

            OnPluginLoaded(new PluginLoadedEventArgs(plugin, dllPath));
            Log($"插件已加载: {plugin.Name} v{plugin.Version}");
        }

        /// <summary>
        /// 注册模板：参数面板模板用复合 Key（类型名 + ".Settings"），
        /// 节点内容模板由插件在 Initialize() 中自行注册（用类型作为隐式 DataType Key）
        /// </summary>
        private void RegisterTemplate(Type operationType, DataTemplate template)
        {
            var key = GetSettingsTemplateKey(operationType);
            if (!Application.Current.Resources.Contains(key))
            {
                Application.Current.Resources.Add(key, template);
            }
            else
            {
                Application.Current.Resources[key] = template;
            }
        }

        /// <summary>获取参数面板模板的 Resource Key</summary>
        public static string GetSettingsTemplateKey(Type operationType)
        {
            return operationType.FullName + ".Settings";
        }

        public void UnloadPlugin(string pluginName)
        {
            if (!_loadedPlugins.TryGetValue(pluginName, out var plugin))
                return;

            try
            {
                plugin.Cleanup();

                foreach (var operationType in plugin.OperationTypes)
                {
                    if (_operationTypeMap.TryGetValue(operationType, out var mappedPlugin) && mappedPlugin == plugin)
                    {
                        _operationTypeMap.Remove(operationType);
                    }

                    var key = GetSettingsTemplateKey(operationType);
                    if (Application.Current.Resources.Contains(key))
                    {
                        Application.Current.Resources.Remove(key);
                    }
                }

                _loadedPlugins.Remove(pluginName);

                var metadata = _pluginMetadata.FirstOrDefault(m => m.Name == pluginName);
                if (metadata != null)
                {
                    _pluginMetadata.Remove(metadata);
                }

                OnPluginUnloaded(new PluginUnloadedEventArgs(plugin));
                Log($"插件已卸载: {pluginName}");
            }
            catch (Exception ex)
            {
                OnPluginError(new PluginErrorEventArgs(pluginName, $"卸载失败: {ex.Message}", ex));
            }
        }

        public void UnloadAllPlugins()
        {
            var pluginNames = _loadedPlugins.Keys.ToList();
            foreach (var name in pluginNames)
            {
                UnloadPlugin(name);
            }
        }

        public void ReloadAllPlugins()
        {
            UnloadAllPlugins();
            LoadAllPlugins();
        }

        public IPlugin? GetPluginForOperation(Type operationType)
        {
            _operationTypeMap.TryGetValue(operationType, out var plugin);
            return plugin;
        }

        public void Log(string message, LogLevel level = LogLevel.Info)
        {
            System.Diagnostics.Debug.WriteLine($"[{DateTime.Now:HH:mm:ss}] [{level}] {message}");
        }

        public void RegisterOperation(Type operationType, IPlugin plugin)
        {
            if (!_operationTypeMap.ContainsKey(operationType))
            {
                _operationTypeMap[operationType] = plugin;
            }
        }

        public IEnumerable<IPlugin> GetPlugins() => _loadedPlugins.Values;

        /// <summary>
        /// 根据类型名称创建操作节点（用于项目加载时反序列化）
        /// </summary>
        /// <param name="typeName">类型的 AssemblyQualifiedName 或 FullName</param>
        /// <param name="title">节点标题</param>
        /// <param name="x">X 坐标</param>
        /// <param name="y">Y 坐标</param>
        /// <returns>创建的 OperationNodeViewModel，失败返回 null</returns>
        public OperationNodeViewModel? CreateNodeFromTypeName(string typeName, string title, double x, double y)
        {
            try
            {
                var type = Type.GetType(typeName);
                if (type == null)
                {
                    // 尝试在所有已加载的程序集中查找
                    foreach (var asm in AppDomain.CurrentDomain.GetAssemblies())
                    {
                        type = asm.GetType(typeName);
                        if (type != null) break;
                    }
                }

                if (type == null)
                {
                    Log($"无法找到类型: {typeName}", LogLevel.Warning);
                    return null;
                }

                var instance = Activator.CreateInstance(type);
                if (instance == null) return null;

                var node = new OperationNodeViewModel(instance);
                node.Title = title;
                node.Location = new System.Windows.Point(x, y);
                return node;
            }
            catch (Exception ex)
            {
                Log($"创建节点失败: {typeName} - {ex.Message}", LogLevel.Error);
                return null;
            }
        }

        public T? GetConfigValue<T>(string key, T? defaultValue = default) => defaultValue;

        public void SetConfigValue<T>(string key, T value) { }

        private void OnPluginLoaded(PluginLoadedEventArgs e) => PluginLoaded?.Invoke(this, e);
        private void OnPluginUnloaded(PluginUnloadedEventArgs e) => PluginUnloaded?.Invoke(this, e);
        private void OnPluginError(PluginErrorEventArgs e) => PluginError?.Invoke(this, e);
    }

    public class PluginLoadedEventArgs : EventArgs
    {
        public IPlugin Plugin { get; }
        public string DllPath { get; }
        public PluginLoadedEventArgs(IPlugin plugin, string dllPath)
        {
            Plugin = plugin;
            DllPath = dllPath;
        }
    }

    public class PluginUnloadedEventArgs : EventArgs
    {
        public IPlugin Plugin { get; }
        public PluginUnloadedEventArgs(IPlugin plugin) => Plugin = plugin;
    }

    public class PluginErrorEventArgs : EventArgs
    {
        public string PluginName { get; }
        public string ErrorMessage { get; }
        public Exception? Exception { get; }
        public PluginErrorEventArgs(string pluginName, string errorMessage, Exception? exception = null)
        {
            PluginName = pluginName;
            ErrorMessage = errorMessage;
            Exception = exception;
        }
    }
}