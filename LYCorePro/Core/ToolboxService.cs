using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using LYCorePro.Core;
using PluginBase.Interfaces;
using PluginBase.Models;

namespace LYCorePro.Core
{
    public class ToolboxService
    {
        private readonly ObservableCollection<ToolboxNode> _rootCategories = new();
        private readonly Dictionary<string, ToolboxCategory> _categoryCache = new();

        public ObservableCollection<ToolboxNode> RootCategories => _rootCategories;

        public void BuildToolbox(IEnumerable<IPlugin> plugins)
        {
            _rootCategories.Clear();
            _categoryCache.Clear();

            var allOperations = new List<(Type Type, PluginMetadata Metadata, IPlugin Plugin)>();

            foreach (var plugin in plugins)
            {
                var metadata = plugin.GetMetadata() as PluginMetadata;
                if (metadata == null) continue;

                foreach (var operationType in plugin.OperationTypes)
                {
                    allOperations.Add((operationType, metadata, plugin));
                }
            }

            foreach (var (type, metadata, plugin) in allOperations.OrderBy(x => x.Metadata.SortOrder))
            {
                AddOperationToTree(type, metadata, plugin);
            }

            SortCategories();
        }

        private void AddOperationToTree(Type operationType, PluginMetadata metadata, IPlugin plugin)
        {
            var categories = metadata.Categories;

            if (!categories.Any())
            {
                AddToCategory("未分类", operationType, metadata, "📂", plugin);
                return;
            }

            foreach (var categoryInfo in categories.OrderBy(c => c.SortOrder))
            {
                AddToCategory(categoryInfo.FullPath, operationType, metadata, "📂", plugin);
            }
        }

        private void AddToCategory(string path, Type operationType, PluginMetadata metadata, string icon, IPlugin plugin)
        {
            var parts = path.Split('/');
            ToolboxCategory? currentCategory = null;

            for (int i = 0; i < parts.Length; i++)
            {
                var part = parts[i];
                var isLast = i == parts.Length - 1;

                var parentKey = string.Join("/", parts.Take(i));
                var currentKey = string.IsNullOrEmpty(parentKey) ? part : $"{parentKey}/{part}";

                if (!_categoryCache.TryGetValue(currentKey, out currentCategory))
                {
                    var parentCategory = string.IsNullOrEmpty(parentKey)
                        ? null
                        : _categoryCache.GetValueOrDefault(parentKey);

                    currentCategory = new ToolboxCategory(part, icon);

                    if (parentCategory != null)
                    {
                        parentCategory.Children.Add(currentCategory);
                    }
                    else
                    {
                        _rootCategories.Add(currentCategory);
                    }

                    _categoryCache[currentKey] = currentCategory;
                }

                if (isLast)
                {
                    var operation = new ToolboxOperation(metadata, operationType, plugin);

                    if (!currentCategory.Children.Any(c =>
                        c is ToolboxOperation op && op.OperationType == operationType))
                    {
                        currentCategory.Children.Add(operation);
                    }
                }
            }
        }

        private void SortCategories()
        {
            SortNode(_rootCategories);
        }

        private void SortNode(ObservableCollection<ToolboxNode> nodes)
        {
            foreach (var node in nodes)
            {
                if (node is ToolboxCategory category)
                {
                    SortNode(category.Children);
                }
            }

            var sorted = nodes
                .OrderBy(n => n is ToolboxCategory ? 0 : 1)
                .ThenBy(n => n.SortOrder)
                .ThenBy(n => n.Name)
                .ToList();

            nodes.Clear();
            foreach (var item in sorted)
            {
                nodes.Add(item);
            }
        }

        public IEnumerable<ToolboxOperation> SearchOperations(string searchText)
        {
            if (string.IsNullOrWhiteSpace(searchText))
                return GetAllOperations();

            searchText = searchText.ToLower();
            return GetAllOperations()
                .Where(op => op.Name.ToLower().Contains(searchText) ||
                            op.Description.ToLower().Contains(searchText) ||
                            op.Tags.Any(t => t.ToLower().Contains(searchText)));
        }

        public IEnumerable<ToolboxOperation> GetAllOperations()
        {
            return GetAllOperationsFromCategory(_rootCategories);
        }

        private IEnumerable<ToolboxOperation> GetAllOperationsFromCategory(
            ObservableCollection<ToolboxNode> nodes)
        {
            foreach (var node in nodes)
            {
                if (node is ToolboxOperation operation)
                {
                    yield return operation;
                }
                else if (node is ToolboxCategory category)
                {
                    foreach (var child in GetAllOperationsFromCategory(category.Children))
                    {
                        yield return child;
                    }
                }
            }
        }

        public object? CreateOperationInstance(Type operationType)
        {
            return Activator.CreateInstance(operationType);
        }
    }
}