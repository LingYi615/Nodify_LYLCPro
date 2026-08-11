using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.Windows;
using System.Windows.Input;
using Nodify;
using PluginBase.Interfaces;

namespace LYCorePro.Core
{
    public class OperationNodeViewModel : ObservableObject
    {
        private object _instance;
        private string _title = string.Empty;
        private ObservableCollection<ConnectorViewModel> _inputs = new();
        private ObservableCollection<ConnectorViewModel> _outputs = new();
        private Point _location;
        private bool _isSelected;
        private bool _isEnabled = true;
        private Size _size;
        private string? _statusMessage;
        private NodeRunStatus _runStatus = NodeRunStatus.Disabled;


        private string _iconData = "M8,0 L16,8 L8,16 L3,16 L11,8 L3,0 Z";
        private ICommand? _executeCommand;

        public object Instance => _instance;

        public string Title
        {
            get => _title;
            set => SetProperty(ref _title, value);
        }

        public ObservableCollection<ConnectorViewModel> Inputs
        {
            get => _inputs;
            set => SetProperty(ref _inputs, value);
        }

        public ObservableCollection<ConnectorViewModel> Outputs
        {
            get => _outputs;
            set => SetProperty(ref _outputs, value);
        }

        public Point Location
        {
            get => _location;
            set => SetProperty(ref _location, value);
        }

        public bool IsSelected
        {
            get => _isSelected;
            set => SetProperty(ref _isSelected, value);
        }

        public bool IsEnabled
        {
            get => _isEnabled;
            set
            {
                if (SetProperty(ref _isEnabled, value))
                    OnPropertyChanged(nameof(BannerText));
            }
        }

        public Size Size
        {
            get => _size;
            set => SetProperty(ref _size, value);
        }

        public string? StatusMessage
        {
            get => _statusMessage;
            set
            {
                if (SetProperty(ref _statusMessage, value))
                {
                    OnPropertyChanged(nameof(BannerText));
                    OnPropertyChanged(nameof(IsErrorBanner));
                }
            }
        }

        public NodeRunStatus RunStatus
        {
            get => _runStatus;
            set
            {
                if (SetProperty(ref _runStatus, value))
                {
                    OnPropertyChanged(nameof(BannerText));
                    OnPropertyChanged(nameof(IsErrorBanner));
                }
            }
        }



        /// <summary>是否为错误消息（红色背景），否则为禁用提示（橙色）</summary>
        public bool IsErrorBanner => _runStatus == NodeRunStatus.Error;

        public string? BannerText
        {
            get
            {
                // 优先显示插件传出的 StatusMessage（格式不固定）
                if (!string.IsNullOrEmpty(_statusMessage))
                    return _statusMessage;

                // 无 StatusMessage 时，根据 RunStatus 显示固定文本
                switch (_runStatus)
                {
                    case NodeRunStatus.Running:
                        return "运行中";
                    case NodeRunStatus.Completed:
                        return "执行成功";
                    case NodeRunStatus.Error:
                        return "错误";
                    case NodeRunStatus.Disabled:
                        return !_isEnabled ? "已禁用" : null;
                    default:
                        return null;
                }
            }
        }

        public string IconData
        {
            get => _iconData;
            set => SetProperty(ref _iconData, value);
        }

        public ICommand? ExecuteCommand
        {
            get => _executeCommand;
            set => SetProperty(ref _executeCommand, value);
        }

        public OperationNodeViewModel(object instance)
        {
            _instance = instance ?? throw new ArgumentNullException(nameof(instance));

            var type = instance.GetType();

            var titleProp = type.GetProperty("Title");
            if (titleProp != null)
            {
                Title = titleProp.GetValue(instance) as string ?? type.Name;
            }
            else
            {
                Title = type.Name;
            }

            var inputsProp = type.GetProperty("Inputs");
            if (inputsProp != null && inputsProp.GetValue(instance) is ObservableCollection<ConnectorViewModel> inputs)
            {
                Inputs = inputs;
            }

            var outputsProp = type.GetProperty("Outputs");
            if (outputsProp != null && outputsProp.GetValue(instance) is ObservableCollection<ConnectorViewModel> outputs)
            {
                Outputs = outputs;
            }

            var iconProp = type.GetProperty("Icon");
            if (iconProp != null && iconProp.GetValue(instance) is string iconData && !string.IsNullOrWhiteSpace(iconData))
            {
                IconData = iconData;
            }

            _location = new Point(0, 0);

            //if (instance is INotifyPropertyChanged notifyInstance)
            //{
            //    notifyInstance.PropertyChanged += (s, e) =>
            //    {
            //        if (e.PropertyName == "StatusMessage")
            //        {
            //            var statusProp = instance.GetType().GetProperty("StatusMessage");
            //            if (statusProp == null)
            //                return;

            //            object propValue = statusProp.GetValue(instance);
            //            string showMsg;
            //            NodeRunStatus targetStatus;

            //            // 先在线程安全的外部完成类型判断，不占用UI调度
            //            if (propValue is NodeRunStatus runStatus)
            //            {
            //                showMsg = runStatus.ToString();
            //                targetStatus = runStatus;
            //            }
            //            else if (propValue is string strMsg)
            //            {
            //                showMsg = strMsg;
            //                targetStatus = NodeRunStatus.Disabled;
            //            }
            //            else
            //            {
            //                showMsg = propValue?.ToString() ?? string.Empty;
            //                targetStatus = NodeRunStatus.Disabled;
            //            }

            //            // UI线程一次性同步更新两个绑定属性，二者同步生效
            //            Application.Current?.Dispatcher.BeginInvoke(() =>
            //            {
            //                StatusMessage = showMsg;
            //                RunStatus = targetStatus;
            //            });
            //        }
            //    };
            //}
        }
    }
}