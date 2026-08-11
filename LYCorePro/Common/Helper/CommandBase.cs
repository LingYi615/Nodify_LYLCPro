using System;
using System.Windows.Input;

namespace LYCorePro.Common.Helper
{
    /// <summary>
    /// 命令基类 - 实现 ICommand 接口
    /// 用于 ViewModel 中的命令绑定
    /// </summary>
    [Serializable]
    public class CommandBase : ICommand
    {
        [field: NonSerialized]
        public event EventHandler CanExecuteChanged;

        public CommandBase() { }

        /// <summary>
        /// 创建始终可执行的命令
        /// </summary>
        public CommandBase(Action<object> doExecute)
        {
            DoExecute = doExecute;
            DoCanExecute = new Func<object, bool>(o => true);
        }

        /// <summary>
        /// 创建带条件判断的命令
        /// </summary>
        public CommandBase(Action<object> doExecute, Func<object, bool> doCanExecute)
        {
            DoExecute = doExecute;
            DoCanExecute = doCanExecute;
        }

        public bool CanExecute(object parameter)
        {
            return DoCanExecute?.Invoke(parameter) == true;
        }

        public void Execute(object parameter = null)
        {
            if (parameter == null)
            {
                DoExecute?.Invoke(null);
            }
            else
            {
                DoExecute?.Invoke(parameter);
            }
        }

        /// <summary>执行委托</summary>
        [field: NonSerialized]
        public Action<object> DoExecute { get; set; }

        /// <summary>可执行条件委托</summary>
        [field: NonSerialized]
        public Func<object, bool> DoCanExecute { get; set; }

        /// <summary>
        /// 手动触发 CanExecuteChanged 事件
        /// </summary>
        public void RaiseCanExecuteChanged()
        {
            CanExecuteChanged?.Invoke(this, new EventArgs());
        }
    }
}