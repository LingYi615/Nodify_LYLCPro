using System;
using System.Collections.Generic;

namespace PluginBase.Interfaces
{
    /// <summary>
    /// 插件元数据接口
    /// </summary>
    public interface IPluginMetadata
    {
        string Name { get; }
        string Description { get; }
        string Version { get; }
        string Author { get; }
        string Icon { get; }
        int SortOrder { get; }
        DateTime LoadTime { get; }
        string AssemblyName { get; }
        IReadOnlyList<CategoryInfo> Categories { get; }
        IReadOnlyList<string> Tags { get; }
        IReadOnlyList<Type> OperationTypes { get; }
    }

    public class CategoryInfo
    {
        public string Category { get; set; } = string.Empty;
        public string SubCategory { get; set; } = string.Empty;
        public string SubSubCategory { get; set; } = string.Empty;
        public int SortOrder { get; set; } = 100;

        public string FullPath => string.IsNullOrEmpty(SubSubCategory)
            ? (string.IsNullOrEmpty(SubCategory) ? Category : $"{Category}/{SubCategory}")
            : $"{Category}/{SubCategory}/{SubSubCategory}";
    }
}