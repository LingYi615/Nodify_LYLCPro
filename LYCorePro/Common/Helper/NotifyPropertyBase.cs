using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace LYCorePro.Common.Helper
{
    /// <summary>
    /// 属性通知基类 - 实现 INotifyPropertyChanged 接口
    /// 所有需要数据绑定的 ViewModel / Model 可继承此类
    /// </summary>
    [Serializable]
    public abstract class NotifyPropertyBase : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;

        /// <summary>
        /// 触发属性变更通知
        /// </summary>
        /// <param name="propertyName">属性名称（自动填充）</param>
        protected virtual void RaisePropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        /// <summary>
        /// 设置属性值并自动触发变更通知
        /// </summary>
        /// <returns>值是否发生变化</returns>
        protected bool SetProperty<T>(ref T field, T value, [CallerMemberName] string propertyName = null)
        {
            if (Equals(field, value)) return false;
            field = value;
            RaisePropertyChanged(propertyName);
            return true;
        }
    }
}