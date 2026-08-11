using System;

namespace PluginBase.Attributes
{
    /// <summary>
    /// 插件标签特性 - 用于搜索和过滤
    /// </summary>
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = true)]
    public sealed class PluginTagAttribute : Attribute
    {
        public string Tag { get; set; }

        public PluginTagAttribute(string tag)
        {
            Tag = tag;
        }
    }
}