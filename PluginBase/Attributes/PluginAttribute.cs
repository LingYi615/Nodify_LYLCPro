using System;

namespace PluginBase.Attributes
{
    /// <summary>
    /// 插件主特性 - 标记一个类为插件
    /// </summary>
    [AttributeUsage(AttributeTargets.Class, Inherited = false)]
    public sealed class PluginAttribute : Attribute
    {
        /// <summary>插件显示名称</summary>
        public string Name { get; set; }

        /// <summary>插件描述</summary>
        public string Description { get; set; } = string.Empty;

        /// <summary>插件版本</summary>
        public string Version { get; set; } = "1.0.0";

        /// <summary>作者</summary>
        public string Author { get; set; } = string.Empty;

        /// <summary>图标</summary>
        public string Icon { get; set; } = string.Empty;

        /// <summary>排序优先级</summary>
        public int SortOrder { get; set; } = 100;

        public PluginAttribute(string name)
        {
            Name = name;
        }
    }
}
