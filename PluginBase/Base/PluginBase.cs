using System;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Xml.Linq;
using PluginBase.Attributes;
using PluginBase.Interfaces;
using PluginBase.Models;
using Nodify;

namespace PluginBase.Base
{
    /// <summary>
    /// 插件基类
    /// </summary>
    public abstract class PluginBase : IPlugin
    {
        protected IPluginHost? Host { get; private set; }
        protected bool IsInitialized { get; private set; }

        public abstract string Name { get; }
        public abstract Version Version { get; }
        public abstract string Description { get; }
        public abstract string Author { get; }
        public abstract Type[] OperationTypes { get; }

        public virtual void Initialize(IPluginHost host)
        {
            Host = host;
            IsInitialized = true;
            Log($"Plugin {Name} initialized");
        }

        public virtual void Cleanup()
        {
            IsInitialized = false;
            Log($"Plugin {Name} cleaned up");
        }

        public virtual DataTemplate? GetTemplate(Type operationType)
        {
            return CreateDefaultNodeTemplate(operationType);
        }

        public virtual object? CreateInstance(Type operationType)
        {
            if (!OperationTypes.Contains(operationType))
                return null;

            return Activator.CreateInstance(operationType);
        }

        public virtual IPluginMetadata GetMetadata()
        {
            var type = GetType();
            var pluginAttr = type.GetCustomAttributes(typeof(PluginAttribute), false)
                .FirstOrDefault() as PluginAttribute;

            var categoryAttrs = type.GetCustomAttributes(typeof(PluginCategoryAttribute), false)
                .Cast<PluginCategoryAttribute>().ToList();

            var tagAttrs = type.GetCustomAttributes(typeof(PluginTagAttribute), false)
                .Cast<PluginTagAttribute>().ToList();

            return new PluginMetadata
            {
                Name = pluginAttr?.Name ?? Name,
                Description = pluginAttr?.Description ?? Description,
                Version = pluginAttr?.Version ?? Version.ToString(),
                Author = pluginAttr?.Author ?? Author,
                Icon = pluginAttr?.Icon ?? string.Empty,
                SortOrder = pluginAttr?.SortOrder ?? 100,
                LoadTime = DateTime.Now,
                AssemblyName = type.Assembly.GetName().Name ?? "Unknown",
                Categories = categoryAttrs.Select(c => new CategoryInfo
                {
                    Category = c.Category,
                    SubCategory = c.SubCategory,
                    SubSubCategory = c.SubSubCategory,
                    SortOrder = c.SortOrder
                }).ToList(),
                Tags = tagAttrs.Select(t => t.Tag).ToList(),
                OperationTypes = OperationTypes.ToList(),
                PluginInstance = this
            };
        }

        protected virtual DataTemplate CreateDefaultNodeTemplate(Type operationType)
        {
            var template = new DataTemplate(operationType);
            var nodeFactory = new FrameworkElementFactory(typeof(Node));

            nodeFactory.SetBinding(Node.HeaderProperty, new Binding("Title"));
            nodeFactory.SetBinding(Node.InputProperty, new Binding("Input"));
            nodeFactory.SetBinding(Node.OutputProperty, new Binding("Output"));

            template.VisualTree = nodeFactory;
            return template;
        }

        protected void Log(string message, LogLevel level = LogLevel.Info)
        {
            Host?.Log($"[{Name}] {message}", level);
        }
    }
}
