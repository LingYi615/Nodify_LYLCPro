using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Linq;
using System.Windows;
using System.Windows.Input;
using Nodify;
using PluginBase.Models;
using PluginBase.Interfaces;
using System.Diagnostics;

namespace LYCorePro.Core
{
    public class EditorViewModel : ObservableObject
    {
        private ObservableCollection<OperationNodeViewModel> _operations = new();
        private ObservableCollection<ConnectionViewModel> _connections = new();
        private ObservableCollection<object> _selectedOperations = new();
        private PendingConnectionViewModel _pendingConnection;
        private double _zoom = 1.0;
        private Point _viewportLocation = new Point(0, 0);
        private ConnectionViewModel? _selectedConnection;

        private (object Instance, Point Offset)[]? _clipboard;

        // 导航栈（子画布展开/返回）
        private readonly Stack<EditorSnapshot> _navigationStack = new();
        private bool _canNavigateBack;
        private string? _currentLevelTitle;

        public ObservableCollection<OperationNodeViewModel> Operations
        {
            get => _operations;
            set
            {
                if (_operations != value)
                {
                    if (_operations != null)
                        _operations.CollectionChanged -= OnOperationsCollectionChanged;
                    _operations = value;
                    if (_operations != null)
                        _operations.CollectionChanged += OnOperationsCollectionChanged;
                    OnPropertyChanged();
                }
            }
        }

        public ObservableCollection<ConnectionViewModel> Connections
        {
            get => _connections;
            set => SetProperty(ref _connections, value);
        }

        public ObservableCollection<object> SelectedOperations
        {
            get => _selectedOperations;
            set => SetProperty(ref _selectedOperations, value);
        }

        public PendingConnectionViewModel PendingConnection
        {
            get => _pendingConnection;
            set => SetProperty(ref _pendingConnection, value);
        }

        public double Zoom
        {
            get => _zoom;
            set => SetProperty(ref _zoom, value);
        }

        public Point ViewportLocation
        {
            get => _viewportLocation;
            set => SetProperty(ref _viewportLocation, value);
        }

        /// <summary>当前选中的连线（用于 Order 编辑）</summary>
        public ConnectionViewModel? SelectedConnection
        {
            get => _selectedConnection;
            set
            {
                if (_selectedConnection == value) return;
                // 取消旧连线的选中状态
                if (_selectedConnection != null)
                    _selectedConnection.IsSelected = false;
                _selectedConnection = value;
                // 设置新连线的选中状态
                if (_selectedConnection != null)
                    _selectedConnection.IsSelected = true;
                OnPropertyChanged();
            }
        }

        /// <summary>是否可以返回上一级画布（子画布展开时可用）</summary>
        public bool CanNavigateBack
        {
            get => _canNavigateBack;
            set => SetProperty(ref _canNavigateBack, value);
        }

        /// <summary>当前层级标题（Navigated 事件中传递）</summary>
        public string? CurrentLevelTitle
        {
            get => _currentLevelTitle;
            set => SetProperty(ref _currentLevelTitle, value);
        }

        /// <summary>导航变更事件（子画布展开/返回时触发）</summary>
        public event Action? Navigated;

        public ICommand DeleteSelectionCommand { get; }
        public ICommand DisconnectConnectorCommand { get; }
        public ICommand ExecuteCommand { get; }
        public ICommand RenameCommand { get; }
        public ICommand DisableCommand { get; }
        public ICommand EnableCommand { get; }
        public ICommand CopyCommand { get; }
        public ICommand PasteCommand { get; }
        public ICommand IncreaseOrderCommand { get; }
        public ICommand DecreaseOrderCommand { get; }
        public ICommand SetParallelCommand { get; }
        public ICommand ExecuteAllCommand { get; }
        public ICommand NavigateBackCommand { get; }
        public ICommand ExpandSubEditorCommand { get; }

        public event Action<OperationNodeViewModel>? RenameRequested;

        public EditorViewModel()
        {
            _pendingConnection = new PendingConnectionViewModel(this);

            DeleteSelectionCommand = new RelayCommand(OnDeleteSelected);
            DisconnectConnectorCommand = new RelayCommand<ConnectorViewModel>(OnDisconnectConnector);
            ExecuteCommand = new RelayCommand<OperationNodeViewModel?>(OnExecute);
            RenameCommand = new RelayCommand(OnRename);
            DisableCommand = new RelayCommand(OnDisable);
            EnableCommand = new RelayCommand(OnEnable);
            CopyCommand = new RelayCommand(OnCopy);
            PasteCommand = new RelayCommand(OnPaste);
            IncreaseOrderCommand = new RelayCommand(OnIncreaseOrder, CanAdjustOrder);
            DecreaseOrderCommand = new RelayCommand(OnDecreaseOrder, CanAdjustOrder);
            SetParallelCommand = new RelayCommand(OnSetParallel, CanAdjustOrder);
            ExecuteAllCommand = new RelayCommand(OnExecuteAll);
            NavigateBackCommand = new RelayCommand(OnNavigateBack);
            ExpandSubEditorCommand = new RelayCommand(OnExpandSubEditor);

            Operations.CollectionChanged += OnOperationsCollectionChanged;
        }

        private void OnOperationsCollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
        {
            if (e.Action == NotifyCollectionChangedAction.Remove && e.OldItems != null)
            {
                foreach (OperationNodeViewModel removedNode in e.OldItems)
                {
                    // 通过反射调用 Disconnect 方法清理通讯实例
                    var disconnectMethod = removedNode.Instance.GetType().GetMethod("Disconnect");
                    if (disconnectMethod != null)
                    {
                        var commInstanceProp = removedNode.Instance.GetType().GetProperty("CommunicationInstance");
                        if (commInstanceProp?.GetValue(removedNode.Instance) != null)
                        {
                            disconnectMethod.Invoke(removedNode.Instance, null);
                            System.Diagnostics.Debug.WriteLine($"[EditorVM] Disconnected on node removal: {removedNode.Title}");
                        }
                    }

                    var connectorIds = removedNode.Inputs.Select(p => p.Id)
                        .Concat(removedNode.Outputs.Select(p => p.Id)).ToHashSet();

                    var toRemove = Connections.Where(conn =>
                        (conn.Output != null && connectorIds.Contains(conn.Output.Id)) ||
                        (conn.Input != null && connectorIds.Contains(conn.Input.Id))).ToList();

                    foreach (var conn in toRemove)
                    {
                        if (conn.Output != null && !Connections.Any(c => c != conn && c.Output == conn.Output))
                            conn.Output.IsConnected = false;
                        if (conn.Input != null && !Connections.Any(c => c != conn && c.Input == conn.Input))
                            conn.Input.IsConnected = false;
                        Connections.Remove(conn);
                    }
                }

                // 重排 Order 确保编号连续
                RenumberConnectionOrders();
            }
        }

        public void CompleteConnection(ConnectorViewModel source, ConnectorViewModel target)
        {
            if (source.Direction != ConnectorDirection.Output || target.Direction != ConnectorDirection.Input)
                return;

            if (source == target)
                return;

            if (!DataTypeHelper.IsCompatible(source.DataType, target.DataType))
            {
                System.Diagnostics.Debug.WriteLine($"[CompleteConnection] Incompatible types");
                return;
            }

            // 禁止多个输出连接到同一个输入（每个输入只能有一条连线）
            var existingInputConnection = Connections.FirstOrDefault(c => c.Input == target);
            if (existingInputConnection != null)
            {
                if (existingInputConnection.Output != null && !Connections.Any(c => c != existingInputConnection && c.Output == existingInputConnection.Output))
                    existingInputConnection.Output.IsConnected = false;
                Connections.Remove(existingInputConnection);
                System.Diagnostics.Debug.WriteLine("[CompleteConnection] 断开目标输入已有连线，替换为新连线");
            }

            // outflag 只能有且只有一条连线（同一输出只能连一个目标）
            if (source.Title.ToLower() == "outflag")
            {
                var existing = Connections.FirstOrDefault(c => c.Output == source);
                if (existing != null)
                {
                    if (existing.Input != null && !Connections.Any(c => c != existing && c.Input == existing.Input))
                        existing.Input.IsConnected = false;
                    Connections.Remove(existing);
                    System.Diagnostics.Debug.WriteLine("[CompleteConnection] 断开 outflag 已有连线");
                }
            }

            var connection = new ConnectionViewModel(target, source);

            // 自动分配 Order：同一输出源的连线按创建顺序递增
            var maxOrder = Connections
                .Where(c => c.Output == source)
                .Select(c => c.Order)
                .DefaultIfEmpty(0)
                .Max();
            connection.Order = maxOrder + 1;

            source.IsConnected = true;
            target.IsConnected = true;

            Connections.Add(connection);

            System.Diagnostics.Debug.WriteLine($"[CompleteConnection] Connection created, total: {Connections.Count}");
        }

        #region 命令实现

        private void OnDeleteSelected()
        {
            var itemsToRemove = SelectedOperations.OfType<OperationNodeViewModel>().ToList();

            // 收集要删除节点的所有连接器 ID，用于后续连线清理
            var connectorIdsToRemove = new HashSet<string>();
            foreach (var node in itemsToRemove)
            {
                foreach (var input in node.Inputs)
                    connectorIdsToRemove.Add(input.Id);
                foreach (var output in node.Outputs)
                    connectorIdsToRemove.Add(output.Id);
            }

            foreach (var item in itemsToRemove)
            {
                Operations.Remove(item);
            }
            SelectedOperations.Clear();

            // 显式清理与已删除节点关联的连线（兜底保障）
            if (connectorIdsToRemove.Count > 0)
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

                if (connsToRemove.Count > 0)
                    RenumberConnectionOrders();
            }
        }

        private void OnDisconnectConnector(ConnectorViewModel? connector)
        {
            if (connector == null) return;

            if (connector.Direction == ConnectorDirection.Output)
                return;

            var connection = Connections.FirstOrDefault(c =>
                c.Input == connector || c.Output == connector);

            if (connection != null)
            {
                if (connection.Input != null && !Connections.Any(c => c != connection && c.Input == connection.Input))
                    connection.Input.IsConnected = false;
                if (connection.Output != null && !Connections.Any(c => c != connection && c.Output == connection.Output))
                    connection.Output.IsConnected = false;
                Connections.Remove(connection);
                System.Diagnostics.Debug.WriteLine($"[DisconnectConnector] Disconnected: {connector.Title}");
            }
        }

        private void OnExecute(OperationNodeViewModel? targetNode)
        {
            var nodes = targetNode != null
                ? new[] { targetNode }
                : SelectedOperations.OfType<OperationNodeViewModel>()
                    .OrderBy(node =>
                    {
                        // 按首个输入连线的 Order 排序，确保同一输出下的多目标按顺序执行
                        var firstConn = Connections
                            .Where(c => node.Inputs.Contains(c.Input))
                            .OrderBy(c => c.Order)
                            .FirstOrDefault();
                        return firstConn?.Order ?? int.MaxValue;
                    })
                    .ToArray();

            foreach (var node in nodes)
            {
                node.RunStatus = NodeRunStatus.Disabled;
                node.StatusMessage = null;

                var invalidInputs = new System.Collections.Generic.List<int>();
                for (int i = 0; i < node.Inputs.Count; i++)
                {
                    var input = node.Inputs[i];
                    if (!input.IsConnected)
                    {
                        invalidInputs.Add(i + 1);
                    }
                    else
                    {
                        var connection = Connections.FirstOrDefault(c => c.Input == input);
                        if (connection?.Output != null)
                        {
                            var sourceNode = Operations.FirstOrDefault(
                                op => op.Outputs.Contains(connection.Output));
                            if (sourceNode != null && !sourceNode.IsEnabled)
                            {
                                invalidInputs.Add(i + 1);
                            }
                            else
                            {
                                // 将源节点输出连接器的 Value 传递到目标节点输入连接器
                                input.Value = connection.Output.Value;
                            }
                        }
                    }
                }

                if (invalidInputs.Count > 0)
                {
                    node.RunStatus = NodeRunStatus.Error;
                    node.StatusMessage = $"输入项#{string.Join("、#", invalidInputs)}无效";
                    System.Diagnostics.Debug.WriteLine($"[Execute] {node.Title}: {node.StatusMessage}");
                }
                else
                {
                    // 通过 IExecutableOperation 接口执行插件操作（通用方式，不依赖具体类型）
                    if (node.Instance is IExecutableOperation execOp)
                    {
                        var success = execOp.Execute();
                        node.RunStatus = execOp.RunStatus;
                        node.StatusMessage = execOp.StatusMessage;
                        System.Diagnostics.Debug.WriteLine($"[Execute] {node.Title}: {(success ? "Success" : "Failed")}");
                    }
                    else
                    {
                        System.Diagnostics.Debug.WriteLine($"[Execute] Executing: {node.Title}");
                    }
                }
            }
        }

        /// <summary>
        /// 执行全部：按拓扑排序 + Order 顺序执行编辑器中所有节点
        /// 同 Order 的节点并行执行（Task.WhenAll）
        /// 已禁用的节点及其所有下游节点均跳过
        /// </summary>
        private async void OnExecuteAll()
        {
            var allNodes = Operations.ToList();
            if (allNodes.Count == 0) return;
            
            // =========全局执行前清空  =========
            foreach (var node in allNodes)
            {
                // 重置节点状态
                node.RunStatus = NodeRunStatus.Disabled;
                node.StatusMessage = null;

                // 清空所有输入连接器旧值
                foreach (var input in node.Inputs)
                {
                    input.Value = null;
                }
                // 清空所有输出连接器旧值 (豁免TCP 串口通讯协议)
                foreach (var output in node.Outputs)
                {
                    if (output.DataType != DataType.TCPAgreement && output.DataType != DataType.SerialAgreement)
                        output.Value = null;
                }
            }

            // 构建完整依赖图（包含所有节点）
            var inDegree = new Dictionary<OperationNodeViewModel, int>();
            var dependents = new Dictionary<OperationNodeViewModel, List<OperationNodeViewModel>>();

            foreach (var node in allNodes)
            {
                inDegree[node] = 0;
                dependents[node] = new List<OperationNodeViewModel>();
            }

            foreach (var conn in Connections)
            {
                var sourceNode = Operations.FirstOrDefault(op => op.Outputs.Contains(conn.Output));
                var targetNode = Operations.FirstOrDefault(op => op.Inputs.Contains(conn.Input));
                if (sourceNode != null && targetNode != null && sourceNode != targetNode)
                {
                    inDegree[targetNode]++;
                    dependents[sourceNode].Add(targetNode);
                }
            }

            // 计算需要跳过的节点：禁用节点 + 所有传递下游节点
            var skipNodes = new HashSet<OperationNodeViewModel>();
            foreach (var node in allNodes.Where(n => !n.IsEnabled))
            {
                var toVisit = new Queue<OperationNodeViewModel>();
                toVisit.Enqueue(node);
                while (toVisit.Count > 0)
                {
                    var current = toVisit.Dequeue();
                    if (skipNodes.Add(current))
                    {
                        foreach (var dep in dependents[current])
                            toVisit.Enqueue(dep);
                    }
                }
            }

            // 入度为 0 且未被跳过的节点作为起始队列
            var queue = new Queue<OperationNodeViewModel>();
            foreach (var node in allNodes)
            {
                if (inDegree[node] == 0 && !skipNodes.Contains(node))
                    queue.Enqueue(node);
            }

            var executed = 0;
            while (queue.Count > 0)
            {
                // 按 Order 排序当前批次
                var batch = queue.OrderBy(node =>
                {
                    var firstConn = Connections
                        .Where(c => node.Inputs.Contains(c.Input))
                        .OrderBy(c => c.Order)
                        .FirstOrDefault();
                    return firstConn?.Order ?? int.MaxValue;
                }).ToList();

                queue.Clear();
                var nextBatch = new List<OperationNodeViewModel>();

                // 按 Order 分组，同 Order 的节点并行执行
                var orderGroups = batch.GroupBy(node =>
                {
                    var firstConn = Connections
                        .Where(c => node.Inputs.Contains(c.Input))
                        .OrderBy(c => c.Order)
                        .FirstOrDefault();
                    return firstConn?.Order ?? int.MaxValue;
                }).OrderBy(g => g.Key);

                foreach (var orderGroup in orderGroups)
                {
                    var tasks = new List<Task>();
                    foreach (var node in orderGroup)
                    {
                        // 传递输入值
                        for (int i = 0; i < node.Inputs.Count; i++)
                        {
                            var input = node.Inputs[i];
                            if (input.IsConnected)
                            {
                                var connection = Connections.FirstOrDefault(c => c.Input == input);
                                if (connection?.Output != null)
                                {
                                    input.Value = connection.Output.Value;
                                }
                            }
                        }
                        // 并行执行
                        tasks.Add(Task.Run(() =>
                        {
                            node.RunStatus = NodeRunStatus.Disabled;
                            node.StatusMessage = null;
                            if (node.Instance is IExecutableOperation execOp)
                            {
                                var success = execOp.Execute();
                                node.RunStatus = execOp.RunStatus;
                                node.StatusMessage = execOp.StatusMessage;
                                System.Diagnostics.Debug.WriteLine($"[ExecuteAll] {node.Title}: {(success ? "Success" : "Failed")}");
                            }
                            else
                            {
                                System.Diagnostics.Debug.WriteLine($"[ExecuteAll] {node.Title}: 不支持执行");
                            }
                        }));
                    }

                    await Task.WhenAll(tasks);

                    foreach (var node in orderGroup)
                    {
                        executed++;
                        // 减少下游节点入度，仅将未被跳过的节点加入下一批
                        foreach (var dep in dependents[node])
                        {
                            inDegree[dep]--;
                            if (inDegree[dep] == 0 && !skipNodes.Contains(dep))
                                nextBatch.Add(dep);
                        }
                    }
                }

                // 下一批节点按 Order 排序后入队
                foreach (var node in nextBatch.OrderBy(n =>
                {
                    var firstConn = Connections
                        .Where(c => n.Inputs.Contains(c.Input))
                        .OrderBy(c => c.Order)
                        .FirstOrDefault();
                    return firstConn?.Order ?? int.MaxValue;
                }))
                {
                    queue.Enqueue(node);
                }
            }

            System.Diagnostics.Debug.WriteLine($"[ExecuteAll] 执行完成: {executed}/{allNodes.Count} 个节点 (跳过 {skipNodes.Count} 个禁用节点及其下游)");
        }

        private void OnRename()
        {
            var selected = SelectedOperations.OfType<OperationNodeViewModel>().FirstOrDefault();
            if (selected == null) return;

            RenameRequested?.Invoke(selected);
        }

        private void OnDisable()
        {
            var selected = SelectedOperations.OfType<OperationNodeViewModel>().ToList();
            foreach (var node in selected)
            {
                node.RunStatus = NodeRunStatus.Disabled;
                node.StatusMessage = "已禁用";
                node.IsEnabled = false;
                System.Diagnostics.Debug.WriteLine($"[Disable] Disabled: {node.Title}");
            }
        }

        private void OnEnable()
        {
            var selected = SelectedOperations.OfType<OperationNodeViewModel>().ToList();
            foreach (var node in selected)
            {
                node.StatusMessage = string.Empty;
                node.IsEnabled = true;
                System.Diagnostics.Debug.WriteLine($"[Enable] Enabled: {node.Title}");
            }
        }

        private void OnCopy()
        {
            var selected = SelectedOperations.OfType<OperationNodeViewModel>().ToList();
            if (selected.Count == 0) return;

            _clipboard = selected.Select(node =>
                (Instance: node.Instance,
                 Offset: new Point(node.Location.X - selected[0].Location.X,
                                   node.Location.Y - selected[0].Location.Y))
            ).ToArray();

            System.Diagnostics.Debug.WriteLine($"[Copy] Copied {_clipboard.Length} nodes");
        }

        private void OnPaste()
        {
            if (_clipboard == null || _clipboard.Length == 0) return;

            var random = new Random();
            var offsetX = random.Next(50, 100);
            var offsetY = random.Next(50, 100);

            var newNodes = new System.Collections.Generic.List<OperationNodeViewModel>();

            foreach (var (instance, offset) in _clipboard)
            {
                var newInstance = Activator.CreateInstance(instance.GetType());
                if (newInstance == null) continue;

                var nodeViewModel = new OperationNodeViewModel(newInstance);
                nodeViewModel.Location = new Point(offset.X + offsetX, offset.Y + offsetY);

                AutoRenameIfNeeded(nodeViewModel);

                nodeViewModel.ExecuteCommand = ExecuteCommand;
                InjectConnections(nodeViewModel);
                InjectParentEditor(nodeViewModel);
                Operations.Add(nodeViewModel);
                newNodes.Add(nodeViewModel);
            }

            _clipboard = newNodes.Select(node =>
                (Instance: node.Instance,
                 Offset: new Point(node.Location.X - newNodes[0].Location.X,
                                   node.Location.Y - newNodes[0].Location.Y))
            ).ToArray();

            SelectedOperations.Clear();
            foreach (var node in newNodes)
            {
                node.IsSelected = true;
            }

            System.Diagnostics.Debug.WriteLine($"[Paste] Pasted {newNodes.Count} nodes");
        }

        #endregion

        #region Order 调整

        private bool CanAdjustOrder()
        {
            return SelectedConnection != null;
        }

        /// <summary>
        /// 上移：将当前连线的 Order 值减 1（与同一输出源下 Order 比它小的连线交换）
        /// </summary>
        private void OnIncreaseOrder()
        {
            if (SelectedConnection == null) return;

            var sameOutput = Connections
                .Where(c => c.Output == SelectedConnection.Output && c != SelectedConnection)
                .OrderBy(c => c.Order)
                .ToList();

            // 找到比当前连线 Order 小的最大 Order（即前一个）
            var prev = sameOutput.LastOrDefault(c => c.Order < SelectedConnection.Order);
            if (prev == null) return;

            // 交换 Order
            (SelectedConnection.Order, prev.Order) = (prev.Order, SelectedConnection.Order);

            System.Diagnostics.Debug.WriteLine(
                $"[Order] 上移: {SelectedConnection.Input.Title} Order={SelectedConnection.Order}");
        }

        /// <summary>
        /// 下移：将当前连线的 Order 值加 1（与同一输出源下 Order 比它大的连线交换）
        /// </summary>
        private void OnDecreaseOrder()
        {
            if (SelectedConnection == null) return;

            var sameOutput = Connections
                .Where(c => c.Output == SelectedConnection.Output && c != SelectedConnection)
                .OrderBy(c => c.Order)
                .ToList();

            // 找到比当前连线 Order 大的最小 Order（即后一个）
            var next = sameOutput.FirstOrDefault(c => c.Order > SelectedConnection.Order);
            if (next == null) return;

            // 交换 Order
            (SelectedConnection.Order, next.Order) = (next.Order, SelectedConnection.Order);

            System.Diagnostics.Debug.WriteLine(
                $"[Order] 下移: {SelectedConnection.Input.Title} Order={SelectedConnection.Order}");
        }

        /// <summary>
        /// 设为并列：将当前连线 Order 设为与上一条连线相同（同时执行）
        /// </summary>
        private void OnSetParallel()
        {
            if (SelectedConnection == null) return;

            var sameOutput = Connections
                .Where(c => c.Output == SelectedConnection.Output && c != SelectedConnection)
                .OrderBy(c => c.Order)
                .ToList();

            // 找到比当前连线 Order 小的最大 Order（前一条连线）
            var prev = sameOutput.LastOrDefault(c => c.Order < SelectedConnection.Order);
            if (prev == null) return;

            SelectedConnection.Order = prev.Order;

            System.Diagnostics.Debug.WriteLine(
                $"[Order] 并列: {SelectedConnection.Input.Title} Order={SelectedConnection.Order} (与 {prev.Input.Title} 并列)");
        }

        #endregion

        /// <summary>
        /// 删除连线后重排同一输出源下所有连线的 Order，确保编号连续（1, 2, 3...）
        /// 保留并列分组：同一 Order 值的连线保持同一分组
        /// </summary>
        public void RenumberConnectionOrders()
        {
            var groups = Connections
                .GroupBy(c => c.Output)
                .Where(g => g.Key != null);

            foreach (var group in groups)
            {
                // 按当前 Order 分组，每组内可能有多个连线（并列）
                var orderGroups = group
                    .GroupBy(c => c.Order)
                    .OrderBy(g => g.Key)
                    .ToList();

                // 重新编号：每组分配一个新的连续序号
                for (int i = 0; i < orderGroups.Count; i++)
                {
                    foreach (var conn in orderGroups[i])
                    {
                        conn.Order = i + 1;
                    }
                }
            }
        }

        /// <summary>
        /// 替换内部连接集合为新实例，用于强制 NodifyEditor 重新渲染连线层。
        /// 会触发 PropertyChanged 通知 NodifyEditorView 重新绑定，
        /// 并同步更新所有插件的 IConnectionAware.Connections 引用。
        /// </summary>
        public void ReplaceConnectionsCollection(ObservableCollection<ConnectionViewModel> newCollection)
        {
            _connections = newCollection ?? new ObservableCollection<ConnectionViewModel>();
            OnPropertyChanged(nameof(Connections));
            UpdatePluginConnectionReferences();
        }

        /// <summary>
        /// 更新所有实现了 IConnectionAware 接口的插件的 Connections 引用。
        /// 防止插件在 ReplaceConnectionsCollection 后持有过期的集合引用。
        /// </summary>
        private void UpdatePluginConnectionReferences()
        {
            foreach (var node in Operations)
            {
                if (node.Instance is IConnectionAware connectionAware)
                {
                    connectionAware.Connections = _connections;
                }
            }
        }

        #region 自动重命名

        private void AutoRenameIfNeeded(OperationNodeViewModel newNode)
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

        public void Cleanup()
        {
            if (Operations != null)
                Operations.CollectionChanged -= OnOperationsCollectionChanged;
        }

        /// <summary>
        /// 向节点实例注入 Connections 引用（如果实例实现了 IConnectionAware 接口）
        /// </summary>
        private void InjectConnections(OperationNodeViewModel node)
        {
            if (node.Instance is IConnectionAware connectionAware)
            {
                connectionAware.Connections = Connections;
                System.Diagnostics.Debug.WriteLine(
                    $"[EditorVM.InjectConnections] 已注入 Connections 引用到: {node.Title}");
            }
        }

        /// <summary>
        /// 向节点实例注入父级 EditorViewModel 引用（用于 FunctionBlock 子画布导航）
        /// </summary>
        private void InjectParentEditor(OperationNodeViewModel node)
        {
            var fbProp = node.Instance.GetType().GetProperty("ParentEditor");
            if (fbProp != null && fbProp.CanWrite)
            {
                fbProp.SetValue(node.Instance, this);
                System.Diagnostics.Debug.WriteLine(
                    $"[EditorVM.InjectParentEditor] 已注入 ParentEditor 到: {node.Title}");
            }
        }


        #region 导航（子画布展开/返回）

        /// <summary>
        /// 导航进入子画布：保存当前状态到栈，替换为子画布的 Operations/Connections
        /// </summary>
        /// <param name="subEditor">子画布的 EditorViewModel</param>
        /// <param name="title">子画布标题（如功能块名称），用于显示</param>
        public void NavigateInto(EditorViewModel subEditor, string title)
        {
            if (subEditor == null) return;

            // 保存当前状态到导航栈
            _navigationStack.Push(new EditorSnapshot
            {
                Operations = new ObservableCollection<OperationNodeViewModel>(Operations),
                Connections = new ObservableCollection<ConnectionViewModel>(Connections),
                PendingConnection = PendingConnection,
                Zoom = Zoom,
                ViewportLocation = ViewportLocation,
                SelectedConnection = SelectedConnection,
                Title = CurrentLevelTitle
            });
            // 清除当前选中（避免选中状态残留）
            SelectedOperations.Clear();

            // 替换为子画布状态
            Operations = subEditor.Operations;
            Connections = subEditor.Connections;
            PendingConnection = subEditor.PendingConnection;
            CanNavigateBack = true;
            CurrentLevelTitle = title;

            // 通知 View 层刷新
            OnPropertyChanged(nameof(Operations));
            OnPropertyChanged(nameof(Connections));
            OnPropertyChanged(nameof(PendingConnection));
            Navigated?.Invoke();

            System.Diagnostics.Debug.WriteLine(
                $"[EditorVM.NavigateInto] 进入子画布: {title}, 栈深度={_navigationStack.Count}");
        }

        /// <summary>
        /// 返回上一级画布
        /// </summary>
        private void OnNavigateBack()
        {
            if (_navigationStack.Count == 0) return;

            var snapshot = _navigationStack.Pop();

            // 恢复父级状态
            Operations = snapshot.Operations;
            Connections = snapshot.Connections;
            PendingConnection = snapshot.PendingConnection;
            Zoom = snapshot.Zoom;
            ViewportLocation = snapshot.ViewportLocation;
            SelectedConnection = snapshot.SelectedConnection;

            CanNavigateBack = _navigationStack.Count > 0;
            CurrentLevelTitle = _navigationStack.Count > 0
                ? _navigationStack.Peek().Title
                : null;

            // 通知 View 层刷新
            OnPropertyChanged(nameof(Operations));
            OnPropertyChanged(nameof(Connections));
            OnPropertyChanged(nameof(PendingConnection));
            OnPropertyChanged(nameof(Zoom));
            OnPropertyChanged(nameof(ViewportLocation));
            Navigated?.Invoke();

            System.Diagnostics.Debug.WriteLine(
                $"[EditorVM.NavigateBack] 返回上一级, 栈深度={_navigationStack.Count}");
        }


        /// <summary>
        /// 返回根画布：清空导航栈，恢复到最顶层画布
        /// 用于加载项目前清理子画布状态，避免新旧数据冲突
        /// </summary>
        public void NavigateToRoot()
        {
            if (_navigationStack.Count == 0) return;

            System.Diagnostics.Debug.WriteLine(
                $"[EditorVM.NavigateToRoot] 清空导航栈 (深度={_navigationStack.Count})，返回根画布");

            // 恢复到最底层的栈顶（根画布），丢弃中间层级
            EditorSnapshot? rootSnapshot = null;
            while (_navigationStack.Count > 0)
            {
                rootSnapshot = _navigationStack.Pop();
            }

            if (rootSnapshot == null) return;

            SelectedOperations.Clear();
            Operations = rootSnapshot.Operations;
            Connections = rootSnapshot.Connections;
            PendingConnection = rootSnapshot.PendingConnection;
            Zoom = rootSnapshot.Zoom;
            ViewportLocation = rootSnapshot.ViewportLocation;
            SelectedConnection = rootSnapshot.SelectedConnection;

            CanNavigateBack = false;
            CurrentLevelTitle = null;

            OnPropertyChanged(nameof(Operations));
            OnPropertyChanged(nameof(Connections));
            OnPropertyChanged(nameof(PendingConnection));
            OnPropertyChanged(nameof(Zoom));
            OnPropertyChanged(nameof(ViewportLocation));
            Navigated?.Invoke();
        }

        /// <summary>
        /// 展开子画布：选中单个功能块节点时，调用其 OpenSubEditor 方法
        /// </summary>
        private void OnExpandSubEditor()
        {
            var selected = SelectedOperations.OfType<OperationNodeViewModel>().FirstOrDefault();
            if (selected == null) return;

            var openMethod = selected.Instance.GetType().GetMethod("OpenSubEditor");
            if (openMethod != null)
            {
                openMethod.Invoke(selected.Instance, null);
                System.Diagnostics.Debug.WriteLine(
                    $"[EditorVM.ExpandSubEditor] 展开子画布: {selected.Title}");
            }
        }

        /// <summary>
        /// 编辑器状态快照（用于导航栈）
        /// </summary>
        private class EditorSnapshot
        {
            public ObservableCollection<OperationNodeViewModel> Operations { get; set; } = new();
            public ObservableCollection<ConnectionViewModel> Connections { get; set; } = new();

            public PendingConnectionViewModel PendingConnection { get; set; } = null!;
            public double Zoom { get; set; } = 1.0;
            public Point ViewportLocation { get; set; }
            public ConnectionViewModel? SelectedConnection { get; set; }
            public string? Title { get; set; }
        }

        #endregion
    }

    public static class DataTypeHelper
    {
        public static bool IsCompatible(DataType source, DataType target)
        {
            // Any 只能连接 Any，所有类型必须严格匹配
            return source == target;
        }
    }
}
