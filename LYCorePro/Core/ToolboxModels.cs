using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using PluginBase.Interfaces;
using PluginBase.Models;

namespace LYCorePro.Core
{
    /// <summary>
    /// 工具箱节点基类
    /// </summary>
    public abstract class ToolboxNode : INotifyPropertyChanged
    {
        private bool _isExpanded = true;
        private bool _isSelected;

        public string Name { get; set; } = string.Empty;
        public string Icon { get; set; } = string.Empty;
        public int SortOrder { get; set; } = 100;
        public string? Tooltip { get; set; }

        public bool IsExpanded
        {
            get => _isExpanded;
            set { _isExpanded = value; OnPropertyChanged(); }
        }

        public bool IsSelected
        {
            get => _isSelected;
            set { _isSelected = value; OnPropertyChanged(); }
        }

        public event PropertyChangedEventHandler? PropertyChanged;
        protected void OnPropertyChanged([CallerMemberName] string? name = null) =>
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
    }

    /// <summary>
    /// 分类节点
    /// </summary>
    public class ToolboxCategory : ToolboxNode
    {
        private ObservableCollection<ToolboxNode> _children = new();

        public ObservableCollection<ToolboxNode> Children
        {
            get => _children;
            set
            {
                _children = value;
                OnPropertyChanged();
            }
        }

        public ToolboxCategory(string name, string icon = "📁")
        {
            Name = name;
            Icon = icon;
        }
    }

    /// <summary>
    /// 操作节点
    /// </summary>
    public class ToolboxOperation : ToolboxNode
    {
        public Type OperationType { get; set; }
        public PluginMetadata Metadata { get; set; }
        public string Description { get; set; } = string.Empty;
        public string Version { get; set; } = string.Empty;
        public int InputCount { get; set; }
        public int OutputCount { get; set; }
        public string[] Tags { get; set; } = Array.Empty<string>();

        public ToolboxOperation(PluginMetadata metadata, Type operationType, IPlugin plugin)
        {
            Metadata = metadata;
            OperationType = operationType;
            Name = metadata.Name;
            Icon = metadata.Icon;
            Description = metadata.Description;
            Version = metadata.Version;
            SortOrder = metadata.SortOrder;
            Tags = metadata.Tags.ToArray();

            var ioAttr = plugin.GetType().GetCustomAttributes(typeof(PluginBase.Attributes.PluginIOAttribute), true)
    .FirstOrDefault() as PluginBase.Attributes.PluginIOAttribute;

            if (ioAttr != null)
            {
                InputCount = ioAttr.InputCount;
                OutputCount = ioAttr.OutputCount;
            }
        }
    }
}