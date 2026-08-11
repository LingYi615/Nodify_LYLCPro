using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Data;
using System.Windows.Input;
using System.Windows.Threading;
using LYCorePro.Common.Helper;
using LYCorePro.Communacation;
using LYCorePro.Core;
using Nodify;
using Opc.Ua;
using PluginBase.Interfaces;
using PluginBase.Models;

namespace DoWhileLoopPlugin
{
    /// <summary>
    /// DoWhile循环（节点实例）
    /// - 拖入编辑器时不带任何输入输出，所有连接器需手动新增
    /// - 输入输出数据类型可自定义（排除 Any）
    /// - 右键 → 展开：在当前 NodifyEditorView 中隐藏父级画布，替换为子画布
    /// - 右键 → 返回上一级：恢复父级画布
    /// - 支持多级嵌套（栈式导航）
    /// - 不包含任何逻辑运算，仅作为容器/子图
    /// </summary>
    public class DoWhileLoopOperationViewModel : NotifyPropertyBase, IExecutableOperation, ISerializableOperation, IConnectionAware
    {
        #region 静态资源

        /// <summary>Output可用数据类型（排除 Any）</summary>
        public static DataType[] AvailableDataTypes { get; } = Enum
            .GetValues(typeof(DataType))
            .Cast<DataType>()
            .Where(dt => dt != DataType.Any)
            .ToArray();
        /// <summary>Input可用数据类型（通讯类数据类型）</summary>
        public static DataType[] InDataTypes { get; } = [DataType.TCPAgreement, DataType.SerialAgreement];

        public static readonly IValueConverter BoolToVisibilityInverseConverter
            = new BoolToVisibilityInverseConverter();

        #endregion

        #region 字段

        private string _title = "DoWhileLoop";
        private string? _statusMessage;
        private string _icon = "M914.286 438.857a36.571 36.571 0 0 1 36.571 36.572v438.857a36.571 36.571 0 0 1-29.988 35.986l-6.583 0.585H196.608l10.46 10.679a36.571 36.571 0 0 1-0.293 51.346l-0.293 0.366a36.133 36.133 0 0 1-51.127 0l-0.292-0.366-69.486-71.095a36.571 36.571 0 0 1 24.137-64.073h768V475.43a36.571 36.571 0 0 1 36.572-36.572z m-46.3-427.3l69.705 69.851 1.098 1.17a36.498 36.498 0 0 1-24.503 63.708h-768V548.57a36.571 36.571 0 0 1-73.143 0V109.714a36.571 36.571 0 0 1 36.571-36.571H826.15l-9.875-9.948a36.571 36.571 0 0 1 0-51.712h0.073a36.571 36.571 0 0 1 51.64 0z M768.293 292.571a36.571 36.571 0 0 1 36.571 36.572v365.714l-0.512 6.876c-5.925 34.889-56.832 41.837-70.217 5.997l-75.776-201.435-75.557 201.435-2.925 6.217C562.03 744.594 512 733.184 512 694.857V329.143l0.585-6.583a36.571 36.571 0 0 1 35.986-29.989l6.583 0.586a36.571 36.571 0 0 1 29.989 35.986v164.644l39.204-104.374 3-6.29a36.571 36.571 0 0 1 65.462 6.29l38.912 103.936V329.143l0.585-6.583a36.571 36.571 0 0 1 35.987-29.989z m-402.579 0a73.143 73.143 0 0 1 73.143 73.143v292.572a73.143 73.143 0 0 1-73.143 73.143H219.43V292.57h146.285z m0 73.143h-73.143v292.572h73.143V365.714z";

        private NodeRunStatus _runStatus;
        // 父级编辑器（由 EditorViewModel/NodifyEditorView 在节点创建时注入）
        private EditorViewModel? _parentEditor;

        // 子画布
        private EditorViewModel? _subEditorViewModel;

        // 连线感知
        private ObservableCollection<ConnectionViewModel> _connections = new();

        #endregion

        #region 节点属性

        public string Title
        {
            get => _title;
            set { SetProperty(ref _title, value);
                RaisePropertyChanged(nameof(InstanceInfo));
            }
        }

        /// <summary>ICON</summary>
        public string Icon
        {
            get => _icon;
            set { SetProperty(ref _icon, value); }
        }

        /// <summary>输入连接器（接收 bool 类型的条件）</summary>
        public ObservableCollection<ConnectorViewModel> Inputs { get; } = new()
        {
            new ConnectorViewModel
            {
                Title = "Step",
                DataType = DataType.Bool,
                Direction = ConnectorDirection.Input,
                IsProtect = true
            }
        };

        /// <summary>输出连接器（初始为空，需通过参数面板新增）</summary>
        public ObservableCollection<ConnectorViewModel> Outputs { get; } = new()
        {
            new ConnectorViewModel
            {
                Title = "Break",
                DataType = DataType.Bool,
                Direction = ConnectorDirection.Output,
                IsProtect = true
            }
        };

        #endregion

        #region ParentEditor（由编辑器注入）

        /// <summary>
        /// 父级 EditorViewModel（由编辑器在节点创建时通过反射注入）
        /// 用于子画布导航（NavigateInto）
        /// </summary>
        public EditorViewModel? ParentEditor
        {
            get => _parentEditor;
            set
            {
                SetProperty(ref _parentEditor, value);
                RaisePropertyChanged(nameof(IsSubEditorOpen));
            }
        }

        /// <summary>是否已展开子画布（当前正在子画布中）</summary>
        public bool IsSubEditorOpen => _parentEditor != null && _parentEditor.CanNavigateBack;

        #endregion

        #region IConnectionAware

        public ObservableCollection<ConnectionViewModel> Connections
        {
            get => _connections;
            set => _connections = value ?? new ObservableCollection<ConnectionViewModel>();
        }

        public void DisconnectConnector(ConnectorViewModel connector)
        {
            if (connector == null) return;

            var relatedConns = Connections.Where(c =>
                c.Input == connector || c.Output == connector).ToList();

            foreach (var conn in relatedConns)
            {
                if (conn.Input != null && !Connections.Any(c => c != conn && c.Input == conn.Input))
                    conn.Input.IsConnected = false;
                if (conn.Output != null && !Connections.Any(c => c != conn && c.Output == conn.Output))
                    conn.Output.IsConnected = false;
                Connections.Remove(conn);
            }
        }

        #endregion

        #region 子画布展开/返回

        /// <summary>子画布 EditorViewModel</summary>
        public EditorViewModel? SubEditorViewModel
        {
            get => _subEditorViewModel;
            set { SetProperty(ref _subEditorViewModel, value);
            }
           
        }

        /// <summary>
        /// 展开子画布：保存当前画布状态到导航栈，在当前 NodifyEditorView 中替换为子画布内容
        /// </summary>
        public void OpenSubEditor()
        {
            if (_parentEditor == null)
            {
                System.Diagnostics.Debug.WriteLine("[DoWhileLoop.OpenSubEditor] ParentEditor 未注入，无法展开");
                return;
            }

            // 创建或获取子画布 ViewModel
            if (_subEditorViewModel == null)
            {
                _subEditorViewModel = new EditorViewModel();
            }

            // 同步入口/出口节点
            SyncEntryExitNodes();

            // 恢复反序列化时暂存的连线数据
            RestoreSubEditorConnections();

            // 导航到子画布
            _parentEditor.NavigateInto(_subEditorViewModel, Title);

            RaisePropertyChanged(nameof(IsSubEditorOpen));
            RaisePropertyChanged(nameof(InstanceInfo));

            System.Diagnostics.Debug.WriteLine($"[DoWhileLoop.OpenSubEditor] 已展开子画布: {Title}");
        }

        /// <summary>
        /// 同步入口/出口节点到子画布（增量更新，保留已有连线）
        /// 入口节点对应 DoWhileLoop 的 Inputs，出口节点对应 DoWhileLoop 的 Outputs
        /// </summary>
        private void SyncEntryExitNodes()
        {
            if (_subEditorViewModel == null) return;

            // 构建现有入口/出口节点字典（按名称索引）
            var existingEntries = _subEditorViewModel.Operations
                .Where(n => n.Instance is SubEditorEntryNode)
                .ToDictionary(n => ((SubEditorEntryNode)n.Instance).NodeName);
            var existingExits = _subEditorViewModel.Operations
                .Where(n => n.Instance is SubEditorExitNode)
                .ToDictionary(n => ((SubEditorExitNode)n.Instance).NodeName);

            // 当前需要的入口名称集合
            var neededEntryNames = new HashSet<string>(Inputs.Select(i => i.Title));
            var neededExitNames = new HashSet<string>(Outputs.Select(o => o.Title));

            // 移除不再需要的入口节点
            foreach (var kv in existingEntries)
            {
                if (!neededEntryNames.Contains(kv.Key))
                    _subEditorViewModel.Operations.Remove(kv.Value);
            }

            // 移除不再需要的出口节点
            foreach (var kv in existingExits)
            {
                if (!neededExitNames.Contains(kv.Key))
                    _subEditorViewModel.Operations.Remove(kv.Value);
            }

            // 添加新的入口节点（左侧排列）
            double x = 100;
            double y = 100;
            foreach (var input in Inputs)
            {
                if (!existingEntries.ContainsKey(input.Title))
                {
                    var entryNode = new SubEditorEntryNode
                    {
                        NodeName = input.Title,
                        DataType = input.DataType
                    };
                    var nodeVm = new OperationNodeViewModel(entryNode);
                    nodeVm.Location = new Point(x, y);
                    nodeVm.ExecuteCommand = _subEditorViewModel.ExecuteCommand;
                    _subEditorViewModel.Operations.Add(nodeVm);
                }
                else
                {
                    // 更新已有节点的 DataType
                    var existing = (SubEditorEntryNode)existingEntries[input.Title].Instance;
                    existing.DataType = input.DataType;
                }
                y += 120;
            }

            // 添加新的出口节点（右侧排列）
            x = 800;
            y = 100;
            foreach (var output in Outputs)
            {
                if (!existingExits.ContainsKey(output.Title))
                {
                    var exitNode = new SubEditorExitNode
                    {
                        NodeName = output.Title,
                        DataType = output.DataType
                    };
                    var nodeVm = new OperationNodeViewModel(exitNode);
                    nodeVm.Location = new Point(x, y);
                    nodeVm.ExecuteCommand = _subEditorViewModel.ExecuteCommand;
                    _subEditorViewModel.Operations.Add(nodeVm);
                }
                else
                {
                    // 更新已有节点的 DataType
                    var existing = (SubEditorExitNode)existingExits[output.Title].Instance;
                    existing.DataType = output.DataType;
                }
                y += 120;
            }
        }

        #endregion

        #region 命令

        private ICommand? _addInputCommand;
        private ICommand? _addOutputCommand;
        private ICommand? _deleteInputCommand;
        private ICommand? _deleteOutputCommand;
        private ICommand? _openSubEditorCommand;

        public ICommand AddInputCommand => _addInputCommand ??= new RelayCommand<DataType?>(AddInput);
        public ICommand AddOutputCommand => _addOutputCommand ??= new RelayCommand<DataType?>(AddOutput);
        public ICommand DeleteInputCommand => _deleteInputCommand ??= new RelayCommand<ConnectorViewModel>(DeleteInput);
        public ICommand DeleteOutputCommand => _deleteOutputCommand ??= new RelayCommand<ConnectorViewModel>(DeleteOutput);
        public ICommand OpenSubEditorCommand => _openSubEditorCommand ??= new RelayCommand(OpenSubEditor);

        private void AddInput(DataType? dataType)
        {
            var dt = dataType ?? DataType.String;
            if (dt == DataType.Any) return;

            var connector = new ConnectorViewModel
            {
                Title = $"输入{Inputs.Count + 1}",
                DataType = dt,
                Direction = ConnectorDirection.Input
            };
            connector.PropertyChanged += OnConnectorPropertyChanged;
            Inputs.Add(connector);

            // 同步到子画布
            if (_subEditorViewModel != null)
                SyncEntryExitNodes();

            RaisePropertyChanged(nameof(InstanceInfo));
        }

        private void AddOutput(DataType? dataType)
        {
            var dt = dataType ?? DataType.String;
            if (dt == DataType.Any) return;

            var connector = new ConnectorViewModel
            {
                Title = $"输出{Outputs.Count + 1}",
                DataType = dt,
                Direction = ConnectorDirection.Output
            };
            connector.PropertyChanged += OnConnectorPropertyChanged;
            Outputs.Add(connector);

            // 同步到子画布
            if (_subEditorViewModel != null)
                SyncEntryExitNodes();

            RaisePropertyChanged(nameof(InstanceInfo));
        }

        private void DeleteInput(ConnectorViewModel? connector)
        {
            if (connector == null || !Inputs.Contains(connector)) return;
            DisconnectConnector(connector);
            Inputs.Remove(connector);

            if (_subEditorViewModel != null)
                SyncEntryExitNodes();

            RaisePropertyChanged(nameof(InstanceInfo));
        }

        private void DeleteOutput(ConnectorViewModel? connector)
        {
            if (connector == null || !Outputs.Contains(connector)) return;
            connector.PropertyChanged -= OnConnectorPropertyChanged;
            DisconnectConnector(connector);
            Outputs.Remove(connector);

            if (_subEditorViewModel != null)
                SyncEntryExitNodes();

            RaisePropertyChanged(nameof(InstanceInfo));
        }

        /// <summary>
        /// 连接器属性变更回调：Title 变更时同步子画布入口/出口节点名称
        /// </summary>
        private void OnConnectorPropertyChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            if (e.PropertyName != nameof(ConnectorViewModel.Title)) return;
            if (sender is not ConnectorViewModel connector) return;
            if (_subEditorViewModel == null) return;

            // 查找输入连接器索引
            var inputIdx = Inputs.IndexOf(connector);
            if (inputIdx >= 0)
            {
                var entryNodes = _subEditorViewModel.Operations
                    .Where(n => n.Instance is SubEditorEntryNode)
                    .ToList();
                if (inputIdx < entryNodes.Count
                    && entryNodes[inputIdx].Instance is SubEditorEntryNode entry)
                {
                    entry.NodeName = connector.Title;
                }
                return;
            }

            // 查找输出连接器索引
            var outputIdx = Outputs.IndexOf(connector);
            if (outputIdx >= 0)
            {
                var exitNodes = _subEditorViewModel.Operations
                    .Where(n => n.Instance is SubEditorExitNode)
                    .ToList();
                if (outputIdx < exitNodes.Count
                    && exitNodes[outputIdx].Instance is SubEditorExitNode exit)
                {
                    exit.NodeName = connector.Title;
                }
            }
        }

        #endregion

        #region IExecutableOperation

        public string? StatusMessage
        {
            get => _statusMessage;
            set
            {
                SetProperty(ref _statusMessage, value);
                RaisePropertyChanged(nameof(InstanceInfo));
            }
        }

        public NodeRunStatus RunStatus
        {
            get => _runStatus;
            set
            {
                SetProperty(ref _runStatus, value);
            }
        }
        private void RunStatusMessage(NodeRunStatus status,string smessage)
        {
            Application.Current?.Dispatcher.BeginInvoke(() =>
            {
                _runStatus = status;
                StatusMessage = smessage;
            });
        }

        public bool Execute()
        {
            _runStatus = NodeRunStatus.Disabled;
            StatusMessage = string.Empty;
            if (Inputs.Count == 0 || Inputs[0].Value is not true)
            {
                RunStatusMessage(NodeRunStatus.Error, "Step未激活");
                return false;
            }
            // ExecuteAll 中通过 Task.Run 在后台线程调用，需封送到 UI 线程
            // 因为子画布中的 Operations/Connections 是 ObservableCollection，只能在 UI 线程修改
            if (System.Windows.Application.Current.Dispatcher.CheckAccess())
            {
                return ExecuteInternal();
            }
            else
            {
                return System.Windows.Application.Current.Dispatcher.Invoke(() => ExecuteInternal());
            }
        }

        private bool ExecuteInternal()
        {
            try
            {
                if (_subEditorViewModel == null || _subEditorViewModel.Operations.Count == 0)
                {
                    RunStatusMessage(NodeRunStatus.Error, "Graph为空");
                    return false;
                }
                // 同步入口/出口节点（仅首次）
                SyncEntryExitNodes();
                // 恢复反序列化时暂存的连线数据（仅首次）
                RestoreSubEditorConnections();

                // 缓存入口/出口节点引用
                var entryNodes = _subEditorViewModel.Operations
                    .Where(n => n.Instance is SubEditorEntryNode)
                    .ToList();
                var exitNodes = _subEditorViewModel.Operations
                    .Where(n => n.Instance is SubEditorExitNode)
                    .ToList();

                var iteration = 0;
                do
                {
                    iteration++;

                    // 每次迭代重置入口节点值（确保子画布拿到最新的输入数据）
                    for (int i = 0; i < Math.Min(Inputs.Count, entryNodes.Count); i++)
                    {
                        if (entryNodes[i].Outputs.Count > 0)
                            entryNodes[i].Outputs[0].Value = Inputs[i].Value;
                        if (entryNodes[i].Instance is SubEditorEntryNode entry)
                            entry.NotifyValueChanged();
                    }

                    // 执行子画布中所有非入口/出口节点（按拓扑排序）
                    var allNodes = _subEditorViewModel.Operations
                        .Where(n => n.Instance is not SubEditorEntryNode && n.Instance is not SubEditorExitNode)
                        .ToList();

                    // 构建依赖图（入度表）
                    var inDegree = new Dictionary<OperationNodeViewModel, int>();
                    var dependents = new Dictionary<OperationNodeViewModel, List<OperationNodeViewModel>>();
                    foreach (var node in allNodes)
                    {
                        inDegree[node] = 0;
                        dependents[node] = new List<OperationNodeViewModel>();
                    }

                    foreach (var conn in _subEditorViewModel.Connections)
                    {
                        var sourceNode = allNodes.FirstOrDefault(op => op.Outputs.Contains(conn.Output));
                        var targetNode = allNodes.FirstOrDefault(op => op.Inputs.Contains(conn.Input));
                        if (sourceNode != null && targetNode != null && sourceNode != targetNode)
                        {
                            inDegree[targetNode]++;
                            dependents[sourceNode].Add(targetNode);
                        }
                    }

                    // 拓扑排序执行
                    var queue = new Queue<OperationNodeViewModel>();
                    foreach (var node in allNodes)
                        if (inDegree[node] == 0)
                            queue.Enqueue(node);

                    while (queue.Count > 0)
                    {
                        var node = queue.Dequeue();

                        // 传递输入值
                        for (int i = 0; i < node.Inputs.Count; i++)
                        {
                            var input = node.Inputs[i];
                            if (input.IsConnected)
                            {
                                var connection = _subEditorViewModel.Connections
                                    .FirstOrDefault(c => c.Input == input);
                                if (connection?.Output != null)
                                    input.Value = connection.Output.Value;
                            }
                        }

                        node.StatusMessage = null;
                        if (node.Instance is IExecutableOperation execOp)
                        {
                            execOp.Execute();
                            node.StatusMessage = execOp.StatusMessage;
                        }

                        foreach (var dep in dependents[node])
                        {
                            inDegree[dep]--;
                            if (inDegree[dep] == 0)
                                queue.Enqueue(dep);
                        }
                    }

                    // 从子画布出口节点读取值到 DoWhileLoop 输出
                    // 注意：出口节点不在拓扑排序中，需从连接源直接读取最新值
                    // 物化为字典避免在循环中枚举 ObservableCollection（防止并发修改导致卡死）
                    var connDict = _subEditorViewModel.Connections
                        .Where(c => c.Input != null)
                        .GroupBy(c => c.Input!)
                        .ToDictionary(g => g.Key, g => g.First().Output);
                    for (int i = 0; i < Math.Min(Outputs.Count, exitNodes.Count); i++)
                    {
                        if (exitNodes[i].Inputs.Count > 0)
                        {
                            var exitInput = exitNodes[i].Inputs[0];
                            // 从连接源输出端读取值，而非依赖 Inputs[0].Value（可能为旧值）
                            if (connDict.TryGetValue(exitInput, out var sourceOutput))
                            {
                                exitInput.Value = sourceOutput?.Value;
                            }
                            Outputs[i].Value = exitInput.Value;
                        }
                        if (exitNodes[i].Instance is SubEditorExitNode exit)
                            exit.NotifyValueChanged();
                    }

                    // 检查循环条件：Break 输出为 false 时退出循环
                    var BreakOutput = Outputs.FirstOrDefault(o =>
                        o.Title.Equals("Break", StringComparison.OrdinalIgnoreCase));
                    if (BreakOutput?.Value is bool breakValue && !breakValue)
                    {
                        RunStatusMessage(NodeRunStatus.Completed, $"执行成功");
                        break;
                    }
                }
                while (true);

                return true;
            }
            catch (Exception ex)
            {
                RunStatusMessage(NodeRunStatus.Error, $"异常: {ex.Message}");
                return false;
            }
        }

        #endregion

        #region ISerializableOperation

        private const int SerializeVersion = 2;

        /// <summary>反序列化时暂存的连线数据，在 OpenSubEditor 中恢复</summary>
        private List<PendingConnectionInfo>? _pendingConnectionData;

        private class PendingConnectionInfo
        {
            public byte SourceType;        // 0=entry node, 1=regular node
            public string? SourceEntryName; // entry node name (if SourceType==0)
            public int SourceNodeIndex;     // regular node index (if SourceType==1)
            public int SourceConnectorIndex;

            public byte TargetType;         // 0=exit node, 1=regular node
            public string? TargetExitName;  // exit node name (if TargetType==0)
            public int TargetNodeIndex;     // regular node index (if TargetType==1)
            public int TargetConnectorIndex;
        }

        public void Serialize(BinaryWriter writer)
        {
            writer.Write(SerializeVersion);
            writer.Write(Title);

            // 输入
            writer.Write(Inputs.Count);
            foreach (var input in Inputs)
            {
                writer.Write(input.Title);
                writer.Write((int)input.DataType);
                writer.Write((bool)input.IsProtect);
            }

            // 输出
            writer.Write(Outputs.Count);
            foreach (var output in Outputs)
            {
                writer.Write(output.Title);
                writer.Write((int)output.DataType);
                writer.Write((bool)output.IsProtect);
            }

            // 子画布状态
            var hasSubEditor = _subEditorViewModel != null && _subEditorViewModel.Operations.Count > 0;
            writer.Write(hasSubEditor);
            if (hasSubEditor)
                SerializeSubEditor(writer);
        }

        public void Deserialize(BinaryReader reader)
        {
            var version = reader.ReadInt32();
            Title = reader.ReadString();

            Inputs.Clear();
            var inputCount = reader.ReadInt32();
            for (int i = 0; i < inputCount; i++)
            {
                Inputs.Add(new ConnectorViewModel
                {
                    Title = reader.ReadString(),
                    DataType = (DataType)reader.ReadInt32(),
                    Direction = ConnectorDirection.Input,
                    IsProtect = reader.ReadBoolean()
                });
            }

            Outputs.Clear();
            var outputCount = reader.ReadInt32();
            for (int i = 0; i < outputCount; i++)
            {
                Outputs.Add(new ConnectorViewModel
                {
                    Title = reader.ReadString(),
                    DataType = (DataType)reader.ReadInt32(),
                    Direction = ConnectorDirection.Output,
                    IsProtect = reader.ReadBoolean()
                });
            }

            var hasSubEditor = reader.ReadBoolean();
            if (hasSubEditor)
                DeserializeSubEditor(reader, version);

            RaisePropertyChanged(nameof(InstanceInfo));
        }

        private void SerializeSubEditor(BinaryWriter writer)
        {
            if (_subEditorViewModel == null) return;

            // 构建节点索引映射（排除入口/出口节点）
            var nodes = _subEditorViewModel.Operations
                .Where(n => n.Instance is not SubEditorEntryNode && n.Instance is not SubEditorExitNode)
                .ToList();

            // 构建入口/出口节点字典
            var entryNodes = _subEditorViewModel.Operations
                .Where(n => n.Instance is SubEditorEntryNode)
                .ToList();
            var exitNodes = _subEditorViewModel.Operations
                .Where(n => n.Instance is SubEditorExitNode)
                .ToList();

            writer.Write(nodes.Count);
            foreach (var node in nodes)
            {
                writer.Write(node.Instance.GetType().AssemblyQualifiedName ?? "");
                writer.Write(node.Instance.GetType().FullName ?? "");
                writer.Write(node.Title);
                writer.Write(node.Location.X);
                writer.Write(node.Location.Y);

                var isSerializable = node.Instance is ISerializableOperation;
                writer.Write(isSerializable);
                if (isSerializable)
                    ((ISerializableOperation)node.Instance).Serialize(writer);
            }

            // 序列化连线数据
            var conns = _subEditorViewModel.Connections
                .Where(c => c.Output != null && c.Input != null)
                .ToList();
            writer.Write(conns.Count);
            foreach (var conn in conns)
            {
                // 源端（输出）
                var sourceEntry = entryNodes.FirstOrDefault(n => n.Outputs.Contains(conn.Output!));
                if (sourceEntry != null)
                {
                    writer.Write((byte)0); // entry node
                    writer.Write(((SubEditorEntryNode)sourceEntry.Instance).NodeName);
                    writer.Write(0); // connector index (entry always has 1 output)
                }
                else
                {
                    var sourceNode = nodes.FirstOrDefault(n => n.Outputs.Contains(conn.Output!));
                    writer.Write((byte)1); // regular node
                    writer.Write(sourceNode != null ? nodes.IndexOf(sourceNode) : -1);
                    writer.Write(sourceNode != null ? sourceNode.Outputs.IndexOf(conn.Output!) : -1);
                }

                // 目标端（输入）
                var targetExit = exitNodes.FirstOrDefault(n => n.Inputs.Contains(conn.Input!));
                if (targetExit != null)
                {
                    writer.Write((byte)0); // exit node
                    writer.Write(((SubEditorExitNode)targetExit.Instance).NodeName);
                    writer.Write(0); // connector index (exit always has 1 input)
                }
                else
                {
                    var targetNode = nodes.FirstOrDefault(n => n.Inputs.Contains(conn.Input!));
                    writer.Write((byte)1); // regular node
                    writer.Write(targetNode != null ? nodes.IndexOf(targetNode) : -1);
                    writer.Write(targetNode != null ? targetNode.Inputs.IndexOf(conn.Input!) : -1);
                }
            }
        }

        private void DeserializeSubEditor(BinaryReader reader, int version)
        {
            if (_subEditorViewModel == null)
                _subEditorViewModel = new EditorViewModel();
            else
                _subEditorViewModel.Operations.Clear();

            var nodeCount = reader.ReadInt32();
            for (int i = 0; i < nodeCount; i++)
            {
                var assemblyQualifiedName = reader.ReadString();
                var fullName = reader.ReadString();
                var title = reader.ReadString();
                var locX = reader.ReadDouble();
                var locY = reader.ReadDouble();

                var type = Type.GetType(assemblyQualifiedName) ?? Type.GetType(fullName);
                if (type != null)
                {
                    var instance = Activator.CreateInstance(type);
                    if (instance != null)
                    {
                        var nodeVm = new OperationNodeViewModel(instance);
                        nodeVm.Title = title;
                        nodeVm.Location = new Point(locX, locY);

                        var isSerializable = reader.ReadBoolean();
                        if (isSerializable && instance is ISerializableOperation serOp)
                            serOp.Deserialize(reader);

                        // 注入子画布的 ExecuteCommand 和 Connections（与拖放/粘贴一致）
                        nodeVm.ExecuteCommand = _subEditorViewModel.ExecuteCommand;
                        if (instance is IConnectionAware connectionAware)
                            connectionAware.Connections = _subEditorViewModel.Connections;

                        _subEditorViewModel.Operations.Add(nodeVm);
                    }
                }
            }

            // 读取连线数据（V2+）
            var connCount = reader.ReadInt32();
            if (connCount > 0)
            {
                _pendingConnectionData = new List<PendingConnectionInfo>();
                for (int i = 0; i < connCount; i++)
                {
                    var info = new PendingConnectionInfo
                    {
                        SourceType = reader.ReadByte()
                    };
                    if (info.SourceType == 0)
                    {
                        info.SourceEntryName = reader.ReadString();
                        info.SourceConnectorIndex = reader.ReadInt32();
                    }
                    else
                    {
                        info.SourceNodeIndex = reader.ReadInt32();
                        info.SourceConnectorIndex = reader.ReadInt32();
                    }

                    info.TargetType = reader.ReadByte();
                    if (info.TargetType == 0)
                    {
                        info.TargetExitName = reader.ReadString();
                        info.TargetConnectorIndex = reader.ReadInt32();
                    }
                    else
                    {
                        info.TargetNodeIndex = reader.ReadInt32();
                        info.TargetConnectorIndex = reader.ReadInt32();
                    }

                    _pendingConnectionData.Add(info);
                }
            }
        }

        /// <summary>
        /// 恢复子画布连线（在 SyncEntryExitNodes 之后调用）
        /// </summary>
        private void RestoreSubEditorConnections()
        {
            if (_subEditorViewModel == null || _pendingConnectionData == null || _pendingConnectionData.Count == 0)
                return;

            // 构建节点索引（排除入口/出口节点）
            var nodes = _subEditorViewModel.Operations
                .Where(n => n.Instance is not SubEditorEntryNode && n.Instance is not SubEditorExitNode)
                .ToList();
            var entryNodes = _subEditorViewModel.Operations
                .Where(n => n.Instance is SubEditorEntryNode)
                .ToDictionary(n => ((SubEditorEntryNode)n.Instance).NodeName);
            var exitNodes = _subEditorViewModel.Operations
                .Where(n => n.Instance is SubEditorExitNode)
                .ToDictionary(n => ((SubEditorExitNode)n.Instance).NodeName);

            foreach (var info in _pendingConnectionData)
            {
                ConnectorViewModel? source = null;
                ConnectorViewModel? target = null;

                // 解析源端
                if (info.SourceType == 0 && info.SourceEntryName != null)
                {
                    if (entryNodes.TryGetValue(info.SourceEntryName, out var entryNode)
                        && entryNode.Outputs.Count > info.SourceConnectorIndex)
                    {
                        source = entryNode.Outputs[info.SourceConnectorIndex];
                    }
                }
                else if (info.SourceType == 1
                    && info.SourceNodeIndex >= 0 && info.SourceNodeIndex < nodes.Count)
                {
                    var node = nodes[info.SourceNodeIndex];
                    if (node.Outputs.Count > info.SourceConnectorIndex)
                        source = node.Outputs[info.SourceConnectorIndex];
                }

                // 解析目标端
                if (info.TargetType == 0 && info.TargetExitName != null)
                {
                    if (exitNodes.TryGetValue(info.TargetExitName, out var exitNode)
                        && exitNode.Inputs.Count > info.TargetConnectorIndex)
                    {
                        target = exitNode.Inputs[info.TargetConnectorIndex];
                    }
                }
                else if (info.TargetType == 1
                    && info.TargetNodeIndex >= 0 && info.TargetNodeIndex < nodes.Count)
                {
                    var node = nodes[info.TargetNodeIndex];
                    if (node.Inputs.Count > info.TargetConnectorIndex)
                        target = node.Inputs[info.TargetConnectorIndex];
                }

                if (source != null && target != null)
                {
                    source.IsConnected = true;
                    target.IsConnected = true;
                    // ConnectionViewModel 构造函数参数顺序: (input, output)
                    _subEditorViewModel.Connections.Add(new ConnectionViewModel(target, source));
                }
            }

            _pendingConnectionData = null;
            System.Diagnostics.Debug.WriteLine(
                $"[DoWhileLoop.RestoreSubEditorConnections] 已恢复子画布连线");
        }

        #endregion

        #region 显示属性

        /// <summary>节点内容预览（含当前数据值）</summary>
        public string InstanceInfo
        {
            get
            {
                var info = $"功能块 [{Title}]";
                if (Inputs.Count > 0)
                    info += $"\n入: {string.Join(", ", Inputs.Select(i => FormatConnectorInfo(i)))}";
                if (Outputs.Count > 0)
                    info += $"\n出: {string.Join(", ", Outputs.Select(o => FormatConnectorInfo(o)))}";
                if (IsSubEditorOpen)
                    info += "\n[子画布已展开]";
                if (!string.IsNullOrEmpty(StatusMessage))
                    info += $"\n{StatusMessage}";
                return info;
            }
        }

        private static string FormatConnectorInfo(ConnectorViewModel c)
        {
            var valStr = c.Value != null ? $"={c.Value}" : "";
            return $"{c.Title}({c.DataType}){valStr}";
        }

        #endregion

        #region INotifyPropertyChanged

        //public event PropertyChangedEventHandler? PropertyChanged;

        //protected void OnPropertyChanged(string propertyName)
        //{
        //    PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        //}

        #endregion
    }

    /// <summary>
    /// 子画布入口节点（数据从 DoWhileLoop Input 流入，向内部节点输出）
    /// </summary>
    public class SubEditorEntryNode : INotifyPropertyChanged
    {
        private string _nodeName = "入口";
        private DataType _dataType = DataType.String;

        public string Title => _nodeName;

        public string NodeName
        {
            get => _nodeName;
            set { _nodeName = value; OnPropertyChanged(nameof(NodeName)); OnPropertyChanged(nameof(Title)); OnPropertyChanged(nameof(DisplayInfo)); }
        }

        public DataType DataType
        {
            get => _dataType;
            set
            {
                _dataType = value;
                if (Outputs.Count > 0) Outputs[0].DataType = value;
                OnPropertyChanged(nameof(DataType));
                OnPropertyChanged(nameof(DisplayInfo));
            }
        }

        /// <summary>当前输出值（执行后更新）</summary>
        public object? Value => Outputs.Count > 0 ? Outputs[0].Value : null;

        /// <summary>节点显示信息</summary>
        public string DisplayInfo => Value != null
            ? $"▶ {NodeName}\n({DataType}) = {Value}"
            : $"▶ {NodeName}\n({DataType})";

        public ObservableCollection<ConnectorViewModel> Inputs { get; } = new();

        public ObservableCollection<ConnectorViewModel> Outputs { get; } = new()
        {
            new ConnectorViewModel
            {
                Title = "输出",
                DataType = DataType.String,
                Direction = ConnectorDirection.Output
            }
        };

        /// <summary>通知值已更新（执行后调用）</summary>
        public void NotifyValueChanged()
        {
            OnPropertyChanged(nameof(Value));
            OnPropertyChanged(nameof(DisplayInfo));
        }

        public event PropertyChangedEventHandler? PropertyChanged;
        protected void OnPropertyChanged(string propertyName)
            => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }

    /// <summary>
    /// 子画布出口节点（从内部节点接收数据，流向 DoWhileLoop Output）
    /// </summary>
    public class SubEditorExitNode : INotifyPropertyChanged
    {
        private string _nodeName = "出口";
        private DataType _dataType = DataType.String;

        public string Title => _nodeName;

        public string NodeName
        {
            get => _nodeName;
            set { _nodeName = value; OnPropertyChanged(nameof(NodeName)); OnPropertyChanged(nameof(Title)); OnPropertyChanged(nameof(DisplayInfo)); }
        }

        public DataType DataType
        {
            get => _dataType;
            set
            {
                _dataType = value;
                if (Inputs.Count > 0) Inputs[0].DataType = value;
                OnPropertyChanged(nameof(DataType));
                OnPropertyChanged(nameof(DisplayInfo));
            }
        }

        /// <summary>当前输入值（执行后更新）</summary>
        public object? Value => Inputs.Count > 0 ? Inputs[0].Value : null;

        /// <summary>节点显示信息</summary>
        public string DisplayInfo => Value != null
            ? $"◀ {NodeName}\n({DataType}) = {Value}"
            : $"◀ {NodeName}\n({DataType})";

        public ObservableCollection<ConnectorViewModel> Inputs { get; } = new()
        {
            new ConnectorViewModel
            {
                Title = "输入",
                DataType = DataType.String,
                Direction = ConnectorDirection.Input
            }
        };

        public ObservableCollection<ConnectorViewModel> Outputs { get; } = new();

        /// <summary>通知值已更新（执行后调用）</summary>
        public void NotifyValueChanged()
        {
            OnPropertyChanged(nameof(Value));
            OnPropertyChanged(nameof(DisplayInfo));
        }

        public event PropertyChangedEventHandler? PropertyChanged;
        protected void OnPropertyChanged(string propertyName)
            => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }

    public class BoolToVisibilityInverseConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is bool b)
                return b ? Visibility.Collapsed : Visibility.Visible;
            return Visibility.Visible;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

}