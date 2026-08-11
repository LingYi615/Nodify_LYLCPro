using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Configuration;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using LYCorePro.Core;

namespace LYCorePro.Views
{
    public partial class NodifyEditorView : UserControl, INotifyPropertyChanged
    {
        #region 依赖属性

        public static readonly DependencyProperty OperationsProperty =
            DependencyProperty.Register(nameof(Operations), typeof(ObservableCollection<OperationNodeViewModel>),
                typeof(NodifyEditorView), new PropertyMetadata(new ObservableCollection<OperationNodeViewModel>()));

        public static readonly DependencyProperty ConnectionsProperty =
            DependencyProperty.Register(nameof(Connections), typeof(ObservableCollection<ConnectionViewModel>),
                typeof(NodifyEditorView), new PropertyMetadata(new ObservableCollection<ConnectionViewModel>()));

        public static readonly DependencyProperty SelectedOperationsProperty =
            DependencyProperty.Register(nameof(SelectedOperations), typeof(ObservableCollection<object>),
                typeof(NodifyEditorView), new PropertyMetadata(new ObservableCollection<object>()));

        public static readonly DependencyProperty ZoomProperty =
            DependencyProperty.Register(nameof(Zoom), typeof(double),
                typeof(NodifyEditorView), new PropertyMetadata(1.0));

        public static readonly DependencyProperty ViewportLocationProperty =
            DependencyProperty.Register(nameof(ViewportLocation), typeof(Point),
                typeof(NodifyEditorView), new PropertyMetadata(new Point(0, 0)));

        public static readonly DependencyProperty PendingConnectionProperty =
            DependencyProperty.Register(nameof(PendingConnection), typeof(PendingConnectionViewModel),
                typeof(NodifyEditorView), new PropertyMetadata(null));

        public static readonly DependencyProperty DisconnectConnectorCommandProperty =
            DependencyProperty.Register(nameof(DisconnectConnectorCommand), typeof(ICommand),
                typeof(NodifyEditorView), new PropertyMetadata(null));

        public static readonly DependencyProperty HasCustomContextMenuProperty =
            DependencyProperty.Register(nameof(HasCustomContextMenu), typeof(bool),
                typeof(NodifyEditorView), new PropertyMetadata(true));

        public static readonly DependencyProperty DeleteSelectionCommandProperty =
            DependencyProperty.Register(nameof(DeleteSelectionCommand), typeof(ICommand),
                typeof(NodifyEditorView), new PropertyMetadata(null));

        public static readonly DependencyProperty CopyCommandProperty =
            DependencyProperty.Register(nameof(CopyCommand), typeof(ICommand),
                typeof(NodifyEditorView), new PropertyMetadata(null));

        public static readonly DependencyProperty PasteCommandProperty =
            DependencyProperty.Register(nameof(PasteCommand), typeof(ICommand),
                typeof(NodifyEditorView), new PropertyMetadata(null));

        public static readonly DependencyProperty ExecuteCommandProperty =
            DependencyProperty.Register(nameof(ExecuteCommand), typeof(ICommand),
                typeof(NodifyEditorView), new PropertyMetadata(null));

        public static readonly DependencyProperty SelectedOperationInstanceProperty =
            DependencyProperty.Register(nameof(SelectedOperationInstance), typeof(object),
                typeof(NodifyEditorView), new PropertyMetadata(null));

        public static readonly DependencyProperty SettingsPanelTitleProperty =
            DependencyProperty.Register(nameof(SettingsPanelTitle), typeof(string),
                typeof(NodifyEditorView), new PropertyMetadata("参数设定"));
        #endregion

        #region 属性

        public ObservableCollection<OperationNodeViewModel> Operations
        {
            get => (ObservableCollection<OperationNodeViewModel>)GetValue(OperationsProperty);
            set => SetValue(OperationsProperty, value);
        }

        public ObservableCollection<ConnectionViewModel> Connections
        {
            get => (ObservableCollection<ConnectionViewModel>)GetValue(ConnectionsProperty);
            set => SetValue(ConnectionsProperty, value);
        }

        public ObservableCollection<object> SelectedOperations
        {
            get => (ObservableCollection<object>)GetValue(SelectedOperationsProperty);
            set => SetValue(SelectedOperationsProperty, value);
        }

        public double Zoom
        {
            get => (double)GetValue(ZoomProperty);
            set => SetValue(ZoomProperty, value);
        }

        public Point ViewportLocation
        {
            get => (Point)GetValue(ViewportLocationProperty);
            set => SetValue(ViewportLocationProperty, value);
        }

        public PendingConnectionViewModel PendingConnection
        {
            get => (PendingConnectionViewModel)GetValue(PendingConnectionProperty);
            set => SetValue(PendingConnectionProperty, value);
        }

        public ICommand DisconnectConnectorCommand
        {
            get => (ICommand)GetValue(DisconnectConnectorCommandProperty);
            set => SetValue(DisconnectConnectorCommandProperty, value);
        }

        public bool HasCustomContextMenu
        {
            get => (bool)GetValue(HasCustomContextMenuProperty);
            set => SetValue(HasCustomContextMenuProperty, value);
        }

        public ICommand DeleteSelectionCommand
        {
            get => (ICommand)GetValue(DeleteSelectionCommandProperty);
            set => SetValue(DeleteSelectionCommandProperty, value);
        }

        public ICommand CopyCommand
        {
            get => (ICommand)GetValue(CopyCommandProperty);
            set => SetValue(CopyCommandProperty, value);
        }

        public ICommand PasteCommand
        {
            get => (ICommand)GetValue(PasteCommandProperty);
            set => SetValue(PasteCommandProperty, value);
        }

        public ICommand ExecuteCommand
        {
            get => (ICommand)GetValue(ExecuteCommandProperty);
            set => SetValue(ExecuteCommandProperty, value);
        }

        public object SelectedOperationInstance
        {
            get => GetValue(SelectedOperationInstanceProperty);
            set => SetValue(SelectedOperationInstanceProperty, value);
        }

        public static readonly DependencyProperty SelectedConnectionProperty =
            DependencyProperty.Register(nameof(SelectedConnection), typeof(ConnectionViewModel),
                typeof(NodifyEditorView), new PropertyMetadata(null));

        public ConnectionViewModel? SelectedConnection
        {
            get => (ConnectionViewModel?)GetValue(SelectedConnectionProperty);
            set => SetValue(SelectedConnectionProperty, value);
        }

        public string SettingsPanelTitle
        {
            get => (string)GetValue(SettingsPanelTitleProperty);
            set => SetValue(SettingsPanelTitleProperty, value);
        }

        public Nodify.NodifyEditor EditorInstance => Editor;

        #endregion

        private const double NodeWidth = 200;
        private const double NodeHeight = 100;
        private bool _isConnectionRefreshPending = false;
        private ObservableCollection<ConnectionViewModel>? _subscribedConnections;

        public NodifyEditorView()
        {
            InitializeComponent();

            DeleteSelectionCommand = new RelayCommand(OnDeleteSelected);

            AllowDrop = true;

            this.DataContextChanged += OnDataContextChanged;
            Editor.ContextMenuOpening += OnContextMenuOpening;

            // 兜底：确保 Delete 键在任何焦点状态下都能触发删除
            this.PreviewKeyDown += OnPreviewKeyDown;

            // 监听选中项变化，控制右侧面板显隐
            SelectedOperations.CollectionChanged += OnSelectedOperationsChanged;
        }

        private void OnDataContextChanged(object sender, DependencyPropertyChangedEventArgs e)
        {
            System.Diagnostics.Debug.WriteLine($"[NodifyEditorView] DataContext changed: {DataContext}");

            if (e.OldValue is EditorViewModel oldVm)
            {
                oldVm.PropertyChanged -= OnEditorViewModelPropertyChanged;
                oldVm.RenameRequested -= OnRenameRequested;
                oldVm.SelectedOperations.CollectionChanged -= OnSelectedOperationsChanged;
            }

            if (e.NewValue is EditorViewModel newVm)
            {
                Operations = newVm.Operations;
                Connections = newVm.Connections;
                SelectedOperations = newVm.SelectedOperations;
                PendingConnection = newVm.PendingConnection;
                DisconnectConnectorCommand = newVm.DisconnectConnectorCommand;
                CopyCommand = newVm.CopyCommand;
                PasteCommand = newVm.PasteCommand;
                ExecuteCommand = newVm.ExecuteCommand;

                newVm.PropertyChanged += OnEditorViewModelPropertyChanged;
                newVm.RenameRequested += OnRenameRequested;
                newVm.Navigated += OnNavigated;
                newVm.SelectedOperations.CollectionChanged += OnSelectedOperationsChanged;

                // 订阅连接集合变更（可重新订阅的命名方法）
                SubscribeToConnections(newVm.Connections);

                UpdateSettingsPanel();
            }
        }

        private void OnEditorViewModelPropertyChanged(object? sender, PropertyChangedEventArgs e)
        {
            if (sender is not EditorViewModel vm) return;

            switch (e.PropertyName)
            {
                case nameof(EditorViewModel.Operations):
                    Operations = vm.Operations;
                    break;
                case nameof(EditorViewModel.Connections):
                    Connections = vm.Connections;
                    // 重新订阅新的连接集合
                    SubscribeToConnections(vm.Connections);
                    break;
                case nameof(EditorViewModel.SelectedOperations):
                    SelectedOperations = vm.SelectedOperations;
                    // 重新订阅 CollectionChanged 以控制右侧面板
                    vm.SelectedOperations.CollectionChanged += OnSelectedOperationsChanged;
                    UpdateSettingsPanel();
                    break;
                case nameof(EditorViewModel.PendingConnection):
                    PendingConnection = vm.PendingConnection;
                    break;
                case nameof(EditorViewModel.Zoom):
                    Zoom = vm.Zoom;
                    break;
                case nameof(EditorViewModel.ViewportLocation):
                    ViewportLocation = vm.ViewportLocation;
                    break;
                case nameof(EditorViewModel.SelectedConnection):
                    UpdateConnectionOrderPanel(vm.SelectedConnection);
                    break;
            }
        }

        #region 重命名对话框

        private void OnRenameRequested(OperationNodeViewModel node)
        {
            var dialog = new Window
            {
                Title = "重命名",
                Width = 320,
                Height = 165,
                WindowStartupLocation = WindowStartupLocation.CenterScreen,
                ResizeMode = ResizeMode.NoResize,
                WindowStyle = WindowStyle.ToolWindow,
                ShowInTaskbar = false
            };


            var grid = new Grid();
            grid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });
            grid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });
            grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            grid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });
            var lb = new Label { Content = "输入名称:", Margin = new Thickness(12, 0, 0, 0), VerticalAlignment = VerticalAlignment.Center, FontSize = 18};
            Grid.SetRow(lb, 0);
            grid.Children.Add(lb);
            var textBox = new TextBox
            {
                Text = node.Title,
                Margin = new Thickness(12, -10, 12, 0),
                VerticalAlignment = VerticalAlignment.Center,
                FontSize = 16,
                MinWidth = 280 ,
                MinHeight = 30
            };
            Grid.SetRow(textBox, 1);
            grid.Children.Add(textBox);

            var buttonPanel = new StackPanel
            {
                Orientation = Orientation.Horizontal,
                HorizontalAlignment = HorizontalAlignment.Right,
                Margin = new Thickness(0, -8, 12, 8)
            };
            Grid.SetRow(buttonPanel, 3);
            grid.Children.Add(buttonPanel);

            var okButton = new Button { Content = "确定", Width = 70, Height = 26, Margin = new Thickness(0, 0, 8, 0) };
            var cancelButton = new Button { Content = "取消", Width = 70, Height = 26 };

            buttonPanel.Children.Add(okButton);
            buttonPanel.Children.Add(cancelButton);

            dialog.Content = grid;

            var result = false;
            var errorText = new TextBlock
            {
                Text = "",
                Foreground = System.Windows.Media.Brushes.Red,
                FontSize = 12,
                Margin = new Thickness(12, 0, 12, 8),
                TextWrapping = TextWrapping.Wrap
            };
            Grid.SetRow(errorText, 2);
            grid.Children.Add(errorText);

            bool ValidateName(string name)
            {
                if (string.IsNullOrWhiteSpace(name)) return false;
                if (name == node.Title) return true; // 未修改，允许
                var exists = Operations.Any(op => op != node && op.Title == name);
                if (exists)
                {
                    errorText.Text = "当前画布已存在此名称，请使用其他名称！";
                    return false;
                }
                errorText.Text = "";
                return true;
            }

            okButton.Click += (s, args) =>
            {
                if (ValidateName(textBox.Text))
                {
                    result = true;
                    dialog.Close();
                }
            };
            cancelButton.Click += (s, args) => dialog.Close();

            textBox.KeyDown += (s, args) =>
            {
                if (args.Key == Key.Enter)
                {
                    if (ValidateName(textBox.Text))
                    {
                        result = true;
                        dialog.Close();
                    }
                }
            };

            dialog.Loaded += (s, args) =>
            {
                textBox.Focus();
                textBox.SelectAll();
            };

            dialog.ShowDialog();

            if (result && !string.IsNullOrWhiteSpace(textBox.Text))
            {
                node.Title = textBox.Text;
            }
        }
        #endregion
        #region 右键菜单

        private void OnContextMenuOpening(object sender, ContextMenuEventArgs e)
        {
            var hasSelection = SelectedOperations != null && SelectedOperations.Count > 0;
            var canNavigateBack = DataContext is EditorViewModel vm && vm.CanNavigateBack;

            var selectedNodes = hasSelection
                ? SelectedOperations.OfType<OperationNodeViewModel>().ToList()
                : new System.Collections.Generic.List<OperationNodeViewModel>();
            var allDisabled = selectedNodes.Count > 0 && selectedNodes.All(n => !n.IsEnabled);
            var allEnabled = selectedNodes.Count > 0 && selectedNodes.All(n => n.IsEnabled);

            MenuItemFitView.Visibility = hasSelection ? Visibility.Collapsed : Visibility.Visible;
            SeparatorEmpty.Visibility = hasSelection ? Visibility.Collapsed : Visibility.Visible;

            MenuItemExecute.Visibility = hasSelection ? Visibility.Visible : Visibility.Collapsed;
            MenuItemExecute.IsEnabled = !allDisabled;

            // 仅选中单个功能块节点时显示"展开子画布"
            var isSingleFunctionBlock = selectedNodes.Count == 1
                && selectedNodes[0].Instance.GetType().GetMethod("OpenSubEditor") != null;
            MenuItemExpandSub.Visibility = isSingleFunctionBlock ? Visibility.Visible : Visibility.Collapsed;

            MenuItemNavigateBack.Visibility = canNavigateBack ? Visibility.Visible : Visibility.Collapsed;

            MenuItemRename.Visibility = hasSelection ? Visibility.Visible : Visibility.Collapsed;
            MenuItemDisable.Visibility = allEnabled ? Visibility.Visible : Visibility.Collapsed;
            MenuItemEnable.Visibility = allDisabled ? Visibility.Visible : Visibility.Collapsed;
            SeparatorNode.Visibility = hasSelection ? Visibility.Visible : Visibility.Collapsed;
            MenuItemCopy.Visibility = hasSelection ? Visibility.Visible : Visibility.Collapsed;
            MenuItemDelete.Visibility = hasSelection ? Visibility.Visible : Visibility.Collapsed;
            SeparatorDelete.Visibility = hasSelection ? Visibility.Visible : Visibility.Collapsed;

            MenuItemPaste.Visibility = Visibility.Visible;
        }

        private void OnFitViewClick(object sender, RoutedEventArgs e)
        {
            FitView();
        }
        /// <summary>
        /// 子画布展开/返回/返回根画布后自动自适应视图
        /// </summary>
        private void OnNavigated()
        {
            Dispatcher.BeginInvoke(new Action(() => CenterView()),
                System.Windows.Threading.DispatcherPriority.Loaded);
        }

        #endregion

        #region 拖放事件处理

        private void OnDragEnter(object sender, DragEventArgs e)
        {
            if (e.Data.GetDataPresent("ToolboxOperation") ||
                e.Data.GetDataPresent("OperationType"))
            {
                e.Effects = DragDropEffects.Copy;
            }
            else
            {
                e.Effects = DragDropEffects.None;
            }
            e.Handled = true;
        }

        private void OnDragOver(object sender, DragEventArgs e)
        {
            if (e.Data.GetDataPresent("ToolboxOperation") ||
                e.Data.GetDataPresent("OperationType"))
            {
                e.Effects = DragDropEffects.Copy;
            }
            else
            {
                e.Effects = DragDropEffects.None;
            }
            e.Handled = true;
        }

        private void OnDrop(object sender, DragEventArgs e)
        {
            try
            {
                Type? operationType = null;
                ToolboxOperation? toolboxOperation = null;

                if (e.Data.GetDataPresent("ToolboxOperation"))
                {
                    toolboxOperation = e.Data.GetData("ToolboxOperation") as ToolboxOperation;
                    if (toolboxOperation != null)
                    {
                        operationType = toolboxOperation.OperationType;
                    }
                }
                else if (e.Data.GetDataPresent("OperationType"))
                {
                    operationType = e.Data.GetData("OperationType") as Type;
                }

                if (operationType == null)
                {
                    e.Effects = DragDropEffects.None;
                    return;
                }

                var position = e.GetPosition(Editor);
                var canvasPosition = Editor.ViewportTransform.Inverse.Transform(position);

                var instance = Activator.CreateInstance(operationType);
                if (instance == null)
                {
                    e.Effects = DragDropEffects.None;
                    return;
                }

                var nodeViewModel = new OperationNodeViewModel(instance);
                nodeViewModel.Location = new Point(
                    canvasPosition.X - NodeWidth / 2,
                    canvasPosition.Y - NodeHeight / 2
                );

                AutoRenameIfNeeded(nodeViewModel);

                nodeViewModel.ExecuteCommand = ExecuteCommand;
                InjectConnections(nodeViewModel);
                InjectParentEditor(nodeViewModel);
                Operations.Add(nodeViewModel);

                e.Effects = DragDropEffects.Copy;
                OnOperationAdded(instance, nodeViewModel.Location);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Drop error: {ex.Message}");
                e.Effects = DragDropEffects.None;
            }

            e.Handled = true;
        }

        internal void AutoRenameIfNeeded(OperationNodeViewModel newNode)
        {
            var baseTitle = newNode.Title;
            var maxNumber = 0;

            foreach (var op in Operations)
            {
                if (op.Title == baseTitle)
                {
                    maxNumber = Math.Max(maxNumber, 1);
                }
                else if (op.Title.StartsWith(baseTitle + "#") &&
                         op.Title.Length > baseTitle.Length + 1 &&
                         int.TryParse(op.Title.AsSpan(baseTitle.Length + 1), out var num))
                {
                    maxNumber = Math.Max(maxNumber, num + 1);
                }
            }

            if (maxNumber > 0)
            {
                newNode.Title = $"{baseTitle}#{maxNumber}";
            }
        }

        #endregion

        #region 命令处理

        private void FitView()
        {
            if (Operations == null || Operations.Count == 0) return;

            double minX = double.MaxValue, minY = double.MaxValue;
            double maxX = double.MinValue, maxY = double.MinValue;

            foreach (var op in Operations)
            {
                if (op.Location.X < minX) minX = op.Location.X;
                if (op.Location.Y < minY) minY = op.Location.Y;
                if (op.Location.X + NodeWidth > maxX) maxX = op.Location.X + NodeWidth;
                if (op.Location.Y + NodeHeight > maxY) maxY = op.Location.Y + NodeHeight;
            }

            if (minX == double.MaxValue || minY == double.MaxValue) return;

            var contentWidth = maxX - minX;
            var contentHeight = maxY - minY;
            var padding = 50.0;

            var viewWidth = Editor.ActualWidth > 0 ? Editor.ActualWidth : 800;
            var viewHeight = Editor.ActualHeight > 0 ? Editor.ActualHeight : 600;

            var scaleX = viewWidth / (contentWidth + padding * 2);
            var scaleY = viewHeight / (contentHeight + padding * 2);
            var scale = Math.Min(scaleX, scaleY);
            scale = Math.Min(scale, 2.0);
            scale = Math.Max(scale, 0.1);

            Zoom = scale;

            var centerX = (minX + maxX) / 2;
            var centerY = (minY + maxY) / 2;

            ViewportLocation = new Point(
                centerX - viewWidth / (2 * scale),
                centerY - viewHeight / (2 * scale)
            );

            if (DataContext is EditorViewModel vm)
            {
                vm.Zoom = scale;
                vm.ViewportLocation = ViewportLocation;
            }
        }

        private void OnConnectionMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (sender is not Nodify.Connection lineConnection
                || lineConnection.DataContext is not ConnectionViewModel conn
                || Connections == null)
                return;

            // 反选所有其他连线
            foreach (var c in Connections)
            {
                c.IsSelected = false;
            }
            // 选中当前连线（同时更新 ViewModel 的 SelectedConnection）
            if (DataContext is EditorViewModel vm)
            {    
                // 选中连线时，清除节点选中状态
                vm.SelectedOperations.Clear();
                foreach (var node in vm.Operations)
                {
                    node.IsSelected = false;
                }
                
                vm.SelectedConnection = conn;
            }
            else
            {
                conn.IsSelected = true;
            }
            // 确保 Editor 获得焦点，以便 Delete 键等 KeyBinding 能触发
            Editor.Focus();
            e.Handled = true;
        }

        private void OnPreviewKeyDown(object sender, KeyEventArgs e)
        {
            // 不干扰文本框编辑（如重命名对话框）
            if (Keyboard.FocusedElement is TextBox)
                return;

            if (e.Key == Key.Delete)
            {
                OnDeleteSelected();
                e.Handled = true;
            }
        }

        private void OnSelectedOperationsChanged(object? sender, NotifyCollectionChangedEventArgs e)
        {
            UpdateSettingsPanel();
        }

        private void UpdateSettingsPanel()
        {
            if (SelectedOperations == null || SelectedOperations.Count != 1)
            {
                SettingsPanel.Visibility = Visibility.Collapsed;
                SelectedOperationInstance = null;
                return;
            }

            if (SelectedOperations[0] is OperationNodeViewModel node)
            {
                // 选中节点时，清除连线选中状态
                if (DataContext is EditorViewModel vm)
                    vm.SelectedConnection = null;
                SettingsContent.Visibility = Visibility.Visible;
                SettingsPanel.Visibility = Visibility.Visible;
                ConnectionOrderPanel.Visibility = Visibility.Collapsed;

                // 按复合 Key 查找插件注册的参数面板 DataTemplate
                var instanceType = node.Instance.GetType();
                var settingsKey = PluginManager.GetSettingsTemplateKey(instanceType);
                DataTemplate? template = null;

                // 先用精确 Key 查找
                if (Application.Current.Resources.Contains(settingsKey))
                {
                    template = Application.Current.Resources[settingsKey] as DataTemplate;
                }

                // 如果精确 Key 没找到，遍历所有资源按 Key 后缀匹配（兼容 FullName 不一致的情况）
                if (template == null)
                {
                    foreach (var key in Application.Current.Resources.Keys)
                    {
                        if (key is string keyStr && keyStr.EndsWith(".Settings") &&
                            keyStr.StartsWith(instanceType.Name) &&
                            Application.Current.Resources[keyStr] is DataTemplate dt)
                        {
                            template = dt;
                            System.Diagnostics.Debug.WriteLine(
                                $"[UpdateSettingsPanel] Fallback 匹配到模板: {keyStr}");
                            break;
                        }
                    }
                }

                // 如果还是没找到，尝试遍历所有资源查找匹配的 DataTemplate
                if (template == null)
                {
                    foreach (var value in Application.Current.Resources.Values)
                    {
                        if (value is DataTemplate dt && dt.DataTemplateKey is Type keyType &&
                            keyType == instanceType)
                        {
                            // 这是隐式 DataTemplate（节点内容模板），不是参数面板模板，跳过
                            continue;
                        }
                    }
                    System.Diagnostics.Debug.WriteLine(
                        $"[UpdateSettingsPanel] 警告: 未找到参数面板模板, Key={settingsKey}");
                }

                // 先设置 ContentTemplate，再设置 Content，避免旧模板应用到新实例造成绑定错误
                SettingsContent.ContentTemplate = template;
                SelectedOperationInstance = node.Instance;

                // 插件可通过 ISettingsPanelProvider 自定义面板标题
                SettingsPanelTitle = node.Instance is ISettingsPanelProvider provider
                    ? provider.SettingsPanelTitle
                    : $"{node.Title} 参数设定";
            }
            else
            {
                SettingsPanel.Visibility = Visibility.Collapsed;
                SelectedOperationInstance = null;
            }
        }

        private void UpdateConnectionOrderPanel(ConnectionViewModel? conn)
        {
            if (conn != null)
            {
                // 选中连线时显示设置面板
                SettingsPanel.Visibility = Visibility.Visible;
                ConnectionOrderPanel.Visibility = Visibility.Visible;
                SettingsContent.Visibility = Visibility.Collapsed;
                SettingsPanelTitle = "连线 Order";
            }
            else
            {
                ConnectionOrderPanel.Visibility = Visibility.Collapsed;
                // 如果没有选中节点，隐藏面板
                if (SelectedOperations == null || SelectedOperations.Count != 1)
                {
                    SettingsPanel.Visibility = Visibility.Collapsed;
                }
            }
        }

        /// <summary>
        /// 订阅连接集合的 CollectionChanged 事件，先取消旧订阅再订阅新集合
        /// </summary>
        private void SubscribeToConnections(ObservableCollection<ConnectionViewModel> connections)
        {
            if (_subscribedConnections != null)
            {
                _subscribedConnections.CollectionChanged -= OnConnectionsCollectionChanged;
            }
            _subscribedConnections = connections;
            if (connections != null)
            {
                connections.CollectionChanged += OnConnectionsCollectionChanged;
            }
        }

        /// <summary>
        /// 当连线集合发生变更时，通过"先清空再恢复"的两步 DP 切换来强制 NodifyEditor 重新渲染连线层。
        /// 第一步：立即将 Connections 设为空集合，强制 Nodify 清除所有视觉连线；
        /// 第二步：调度 Dispatcher 回调，将 Connections 恢复为实际连线集合，强制 Nodify 重新渲染。
        /// 同时更新所有插件的 IConnectionAware.Connections 引用，防止插件持有过期集合引用。
        /// </summary>
        private void OnConnectionsCollectionChanged(object? sender, NotifyCollectionChangedEventArgs args)
        {
            System.Diagnostics.Debug.WriteLine($"[NodifyEditorView] Connections changed. Action: {args.Action}, Count: {((sender as ObservableCollection<ConnectionViewModel>)?.Count ?? 0)}");

            if (args.Action == NotifyCollectionChangedAction.Remove ||
                args.Action == NotifyCollectionChangedAction.Reset)
            {
                if (!_isConnectionRefreshPending)
                {
                    _isConnectionRefreshPending = true;

                    // Step 1: 立即清空，强制 NodifyEditor 清除所有视觉连线
                    Connections = new ObservableCollection<ConnectionViewModel>();

                    // Step 2: 调度 Dispatcher 回调恢复实际连线（确保 Nodify 先处理了清空操作）
                    Dispatcher.BeginInvoke(new Action(() =>
                    {
                        if (DataContext is EditorViewModel vm)
                        {
                            var actualCollection = new ObservableCollection<ConnectionViewModel>(vm.Connections);
                            vm.ReplaceConnectionsCollection(actualCollection);
                            // Connections DP 已在 OnEditorViewModelPropertyChanged 中更新，
                            // 但此处再显式设置一次以确保 NodifyEditor 绑定刷新
                            Connections = actualCollection;
                        }
                        _isConnectionRefreshPending = false;
                    }), System.Windows.Threading.DispatcherPriority.Background);
                }
            }
        }

        private void OnDeleteSelected()
        {
            // 删除选中的连线
            if (Connections != null)
            {
                var selectedConns = Connections.Where(c => c.IsSelected).ToList();
                foreach (var conn in selectedConns)
                {
                    if (conn.Input != null && !Connections.Any(c => c != conn && c.Input == conn.Input))
                        conn.Input.IsConnected = false;
                    if (conn.Output != null && !Connections.Any(c => c != conn && c.Output == conn.Output))
                        conn.Output.IsConnected = false;
                    Connections.Remove(conn);
                }
                // 清除连线的选中状态 + 重排 Order
                if (DataContext is EditorViewModel vm)
                {
                    vm.SelectedConnection = null;
                    vm.RenumberConnectionOrders();
                }
            }

            // 删除选中的节点
            if (SelectedOperations != null)
            {
                var itemsToRemove = new List<OperationNodeViewModel>();
                foreach (var selected in SelectedOperations)
                {
                    if (selected is OperationNodeViewModel node)
                    {
                        // 通过反射调用 Disconnect 方法清理通讯实例
                        var disconnectMethod = node.Instance.GetType().GetMethod("Disconnect");
                        if (disconnectMethod != null)
                        {
                            var commInstanceProp = node.Instance.GetType().GetProperty("CommunicationInstance");
                            if (commInstanceProp?.GetValue(node.Instance) != null)
                            {
                                disconnectMethod.Invoke(node.Instance, null);
                            }
                        }
                        itemsToRemove.Add(node);
                    }
                }

                // 收集要删除节点的所有连接器 ID，用于后续连线清理
                var connectorIdsToRemove = new HashSet<string>();
                foreach (var node in itemsToRemove)
                {
                    foreach (var input in node.Inputs)
                        connectorIdsToRemove.Add(input.Id);
                    foreach (var output in node.Outputs)
                        connectorIdsToRemove.Add(output.Id);
                }

                foreach (var node in itemsToRemove)
                {
                    Operations?.Remove(node);
                }
                SelectedOperations?.Clear();

                // 显式清理与已删除节点关联的连线（兜底保障，确保 Nodify 视觉同步）
                if (Connections != null && connectorIdsToRemove.Count > 0)
                {
                    var connsToRemove = Connections.Where(c =>
                        (c.Output != null && connectorIdsToRemove.Contains(c.Output.Id)) ||
                        (c.Input != null && connectorIdsToRemove.Contains(c.Input.Id))).ToList();

                    foreach (var conn in connsToRemove)
                    {
                        if (conn.Output != null && !Connections.Any(c => c != conn && c.Output == conn.Output))
                            conn.Output.IsConnected = false;
                        if (conn.Input != null && !Connections.Any(c => c != conn && c.Input == conn.Input))
                            conn.Input.IsConnected = false;
                        Connections.Remove(conn);
                    }

                    if (connsToRemove.Count > 0 && DataContext is EditorViewModel vm2)
                    {
                        vm2.RenumberConnectionOrders();
                    }
                }
            }
        }

        #endregion

        #region 公共方法

        /// <summary>
        /// 向节点实例注入 Connections 引用（如果实例实现了 IConnectionAware 接口）
        /// </summary>
        private void InjectConnections(OperationNodeViewModel node)
        {
            if (node.Instance is IConnectionAware connectionAware)
            {
                connectionAware.Connections = Connections;
                System.Diagnostics.Debug.WriteLine(
                    $"[InjectConnections] 已注入 Connections 引用到: {node.Title}");
            }
        }

        /// <summary>
        /// 向 FunctionBlockOperationViewModel 实例注入父级 EditorViewModel 引用
        /// 用于子画布导航（NavigateInto）
        /// </summary>
        private void InjectParentEditor(OperationNodeViewModel node)
        {
            if (DataContext is EditorViewModel parentVm)
            {
                var fbProp = node.Instance.GetType().GetProperty("ParentEditor");
                if (fbProp != null && fbProp.CanWrite)
                {
                    fbProp.SetValue(node.Instance, parentVm);
                    System.Diagnostics.Debug.WriteLine(
                        $"[InjectParentEditor] 已注入 ParentEditor 到: {node.Title}");
                }
            }
        }

        public void AddOperation(object instance, Point? location = null)
        {
            if (instance == null) return;

            var nodeViewModel = new OperationNodeViewModel(instance);

            if (location.HasValue)
            {
                nodeViewModel.Location = location.Value;
            }
            else
            {
                var random = new Random();
                nodeViewModel.Location = new Point(
                    random.Next(50, 500),
                    random.Next(50, 500)
                );
            }

            AutoRenameIfNeeded(nodeViewModel);
            nodeViewModel.ExecuteCommand = ExecuteCommand;
            InjectConnections(nodeViewModel);
            InjectParentEditor(nodeViewModel);
            Operations?.Add(nodeViewModel);
        }

        public void ClearOperations()
        {
            Operations?.Clear();
            Connections?.Clear();
            SelectedOperations?.Clear();
        }

        public void CenterView()
        {
            FitView();
        }

        public void ResetView()
        {
            Zoom = 1.0;
            ViewportLocation = new Point(0, 0);
        }

        #endregion

        #region 事件

        public event EventHandler<OperationAddedEventArgs>? OperationAdded;

        protected virtual void OnOperationAdded(object instance, Point position)
        {
            OperationAdded?.Invoke(this, new OperationAddedEventArgs(instance, position));
        }

        #endregion

        #region INotifyPropertyChanged

        public event PropertyChangedEventHandler? PropertyChanged;
        protected void OnPropertyChanged([CallerMemberName] string? name = null) =>
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));

        #endregion
    }

    public class OperationAddedEventArgs : EventArgs
    {
        public object Instance { get; }
        public Point Position { get; }

        public OperationAddedEventArgs(object instance, Point position)
        {
            Instance = instance;
            Position = position;
        }
    }
}