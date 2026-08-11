using System;
using System.Collections.Generic;
using System.Windows;

namespace PluginBase.Interfaces
{
    /// <summary>
    /// 操作视图模型基础接口
    /// </summary>
    public interface IOperationViewModel
    {
        string Title { get; }
        object? Input { get; set; }
        object? Output { get; set; }
        Point Location { get; set; }
    }

    /// <summary>
    /// 可定位的接口
    /// </summary>
    public interface ICanPosition
    {
        Point Location { get; set; }
    }
}