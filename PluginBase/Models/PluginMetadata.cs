using System;
using System.Collections.Generic;
using PluginBase.Interfaces;

namespace PluginBase.Models
{
    /// <summary>
    /// 插件元数据实现
    /// </summary>
    public class PluginMetadata : IPluginMetadata
    {
        public string Name { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string Version { get; set; } = string.Empty;
        public string Author { get; set; } = string.Empty;
        public string Icon { get; set; } = string.Empty;
        public int SortOrder { get; set; } = 100;
        public DateTime LoadTime { get; set; }
        public string AssemblyName { get; set; } = string.Empty;
        public IReadOnlyList<CategoryInfo> Categories { get; set; } = new List<CategoryInfo>();
        public IReadOnlyList<string> Tags { get; set; } = new List<string>();
        public IReadOnlyList<Type> OperationTypes { get; set; } = new List<Type>();
        public object PluginInstance { get; set; } = null!;
    }
}