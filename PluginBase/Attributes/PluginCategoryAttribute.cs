using System;

namespace PluginBase.Attributes
{
    /// <summary>
    /// 插件分类特性 - 用于构建树状结构
    /// </summary>
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = true)]
    public sealed class PluginCategoryAttribute : Attribute
    {
        /// <summary>一级分类</summary>
        public string Category { get; set; }

        /// <summary>二级分类</summary>
        public string SubCategory { get; set; } = string.Empty;

        /// <summary>三级分类</summary>
        public string SubSubCategory { get; set; } = string.Empty;

        /// <summary>分类排序</summary>
        public int SortOrder { get; set; } = 100;

        public PluginCategoryAttribute(string category)
        {
            Category = category;
        }

        public PluginCategoryAttribute(string category, string subCategory)
        {
            Category = category;
            SubCategory = subCategory;
        }

        public PluginCategoryAttribute(string category, string subCategory, string subSubCategory)
        {
            Category = category;
            SubCategory = subCategory;
            SubSubCategory = subSubCategory;
        }
    }
}