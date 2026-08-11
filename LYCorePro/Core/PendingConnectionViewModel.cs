using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using Nodify;
using PluginBase.Models;

namespace LYCorePro.Core
{
    /// <summary>
    /// 待处理连接视图模型
    /// 管理拖拽连线过程中的状态和类型兼容性检查
    /// </summary>
    public class PendingConnectionViewModel : ObservableObject
    {
        private readonly EditorViewModel _editor;
        private bool _isVisible;
        private ConnectorViewModel? _source;
        private ConnectorViewModel? _target;
        private Point _targetLocation;
        private bool _isCompatible = true;

        public bool IsVisible
        {
            get => _isVisible;
            set => SetProperty(ref _isVisible, value);
        }

        public ConnectorViewModel? Source
        {
            get => _source;
            set
            {
                if (SetProperty(ref _source, value))
                    UpdateCompatibility();
            }
        }

        public ConnectorViewModel? Target
        {
            get => _target;
            set
            {
                if (SetProperty(ref _target, value))
                    UpdateCompatibility();
            }
        }

        public Point TargetLocation
        {
            get => _targetLocation;
            set => SetProperty(ref _targetLocation, value);
        }

        /// <summary>当前源和目标的数据类型是否兼容</summary>
        public bool IsCompatible
        {
            get => _isCompatible;
            set
            {
                if (SetProperty(ref _isCompatible, value))
                    OnPropertyChanged(nameof(ConnectionStroke));
            }
        }

        /// <summary>
        /// 连线颜色：兼容时显示蓝色，不兼容时显示红色
        /// </summary>
        public Brush ConnectionStroke =>
            IsCompatible
                ? new SolidColorBrush(Color.FromRgb(0x4A, 0x90, 0xD9))
                : new SolidColorBrush(Color.FromRgb(0xFF, 0x55, 0x55));

        public ICommand StartConnectionCommand { get; }
        public ICommand CompleteConnectionCommand { get; }

        public PendingConnectionViewModel(EditorViewModel editor)
        {
            _editor = editor;
            StartConnectionCommand = new RelayCommand<ConnectorViewModel>(OnStartConnection);
            CompleteConnectionCommand = new RelayCommand<ConnectorViewModel>(OnCompleteConnection);
        }

        private void OnStartConnection(ConnectorViewModel? connector)
        {
            if (connector == null)
                return;

            Source = connector;
            Target = null;
            IsVisible = true;
            IsCompatible = true;
        }

        private void OnCompleteConnection(ConnectorViewModel? connector)
        {
            if (Source == null || connector == null)
            {
                IsVisible = false;
                Source = null;
                return;
            }

            // 如果从输入拖到输出，自动翻转方向，确保输出→输入
            var output = Source.Direction == ConnectorDirection.Output ? Source : connector;
            var input = Source.Direction == ConnectorDirection.Input ? Source : connector;

            // 检查数据类型兼容性
            if (!DataTypeHelper.IsCompatible(output.DataType, input.DataType))
            {
                // 类型不兼容：短暂显示红色连线后消失
                Target = connector;
                IsCompatible = false;
                System.Diagnostics.Debug.WriteLine(
                    $"[PendingConnection] Incompatible types: {output.DataType} -> {input.DataType}");

                // 延迟隐藏，让用户看到红色反馈
                _ = System.Threading.Tasks.Task.Delay(300).ContinueWith(_ =>
                {
                    Application.Current?.Dispatcher.Invoke(() =>
                    {
                        IsVisible = false;
                        Source = null;
                        Target = null;
                        IsCompatible = true;
                    });
                });
                return;
            }

            _editor.CompleteConnection(output, input);

            IsVisible = false;
            Source = null;
        }

        /// <summary>更新兼容性状态</summary>
        private void UpdateCompatibility()
        {
            if (Source == null || Target == null)
            {
                IsCompatible = true;
                return;
            }

            var output = Source.Direction == ConnectorDirection.Output ? Source : Target;
            var input = Source.Direction == ConnectorDirection.Input ? Source : Target;

            IsCompatible = DataTypeHelper.IsCompatible(output.DataType, input.DataType);
        }
    }
}