using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Data;
using System.Windows.Input;
using LYCorePro.Common.Helper;
using LYCorePro.Core;
using Nodify;
using PluginBase.Interfaces;
using PluginBase.Models;

namespace IfPlugin
{
    /// <summary>
    /// 输入分支的 ViewModel 包装（用于 XAML 绑定取反复选框）
    /// </summary>
    public class InputBranchViewModel : INotifyPropertyChanged
    {
        private readonly IfOperationViewModel _owner;
        private bool _invert;

        public ConnectorViewModel Connector { get; }
        public string BranchLabel { get; }
        public bool IsDeletable { get; }

        public bool Invert
        {
            get => _invert;
            set
            {
                if (_invert != value)
                {
                    _invert = value;
                    _owner.SetInvert(Connector, value);
                    OnPropertyChanged(nameof(Invert));
                }
            }
        }

        public void SyncInvert(bool value)
        {
            if (_invert != value)
            {
                _invert = value;
                OnPropertyChanged(nameof(Invert));
            }
        }

        public InputBranchViewModel(IfOperationViewModel owner, ConnectorViewModel connector, string label, bool isDeletable, bool invert)
        {
            _owner = owner;
            Connector = connector;
            BranchLabel = label;
            IsDeletable = isDeletable;
            _invert = invert;
        }

        public event PropertyChangedEventHandler? PropertyChanged;
        protected void OnPropertyChanged(string propertyName)
            => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }

    /// <summary>
    /// IF 条件操作（节点实例）
    /// - 所有分支共用同一个子画布
    /// - Inputs 中 DataType.Bool 且存在 _invertFlags 的为分支条件（IF/ELSE IF），其余为数据输入
    /// - 分支条件始终 bool，可设置取反
    /// - 数据输入为任意类型，流入所有分支
    /// - 无 ELSE 分支
    /// - 默认输出 "Flow"（bool），执行前为 false，执行后为 true
    /// - 可新增其他输出传递数据
    /// </summary>
    public class IfOperationViewModel : NotifyPropertyBase, IExecutableOperation, ISerializableOperation, IConnectionAware
    {
        #region 静态资源

        public static DataType[] AvailableDataTypes { get; } = Enum
            .GetValues(typeof(DataType))
            .Cast<DataType>()
            .Where(dt => dt != DataType.Any)
            .ToArray();

        public static readonly IValueConverter BoolToVisibilityInverseConverter
            = new BoolToVisibilityInverseConverter();
        #endregion

        #region 字段

        private string _title = "IF条件";
        private string? _statusMessage;
        private NodeRunStatus _runStatus;

        private EditorViewModel? _parentEditor;
        private EditorViewModel? _subEditorViewModel;
        private ObservableCollection<ConnectionViewModel> _connections = new();

        /// <summary>每个分支条件连接器的取反标记（有 key 的即为分支条件）</summary>
        private readonly Dictionary<ConnectorViewModel, bool> _invertFlags = new();

        /// <summary>输入分支 ViewModel 列表（仅分支条件，用于设置面板绑定）</summary>
        private readonly ObservableCollection<InputBranchViewModel> _branchInputs = new();

        private string _icon = "M714.176 108.928c-3.008 29.568-46.272 26.496-57.728 26.496-69.824 0-145.664-36.224-186.368 112.96l-19.008 93.568H547.2c17.6 0 32 14.336 32 32s-14.4 32-32 32H437.44l-86.4 424.384s-8.32 68.224-50.24 65.6c-48-3.008-25.984-71.296-25.984-71.296l85.632-418.688H294.4c-16.704 0-30.016-12.864-31.424-29.248 1.408-29.952 14.72-34.752 31.424-34.752h79.808l20.032-95.488c14.208-67.648 43.52-124.48 94.592-157.504 51.008-33.024 134.976-27.008 175.104-22.464 16.896 1.92 54.016 5.376 50.24 42.432zM198.272 376.576c3.84-17.344-7.04-34.304-24.384-38.144-17.28-3.84-34.304 7.104-38.144 24.384L26.816 852.672c-3.648 16.32-1.664 30.4 27.2 38.208 16.32 2.112 31.808-8.128 35.328-24.384l108.928-489.92zm64.128 4.736c0-1.92 0.448-2.88 0.576-4.608-0.192-0.96-0.576-1.792-0.576-2.752v7.36zm-61.632-209.664c-26.496 0-48 21.504-48 48s21.504 48 48 48c26.624 0 48-21.504 48-48s-21.376-48-48-48zm479.936 712.448c16.384-13.696 20.736-40.576 9.856-60.032-0.384-0.832-43.648-79.232-36.736-191.616 6.848-109.696 71.36-184.64 72.896-186.368 14.528-16.32 15.616-43.52 2.368-60.736-13.376-17.152-36.096-17.984-50.752-1.6-3.52 4.032-86.976 98.752-95.808 244.288-8.896 143.296 46.336 241.6 48.576 245.824 6.4 11.008 16.384 17.344 27.008 17.984 7.552 0.512 15.68-1.984 22.592-7.744zm188.864 4.544c10.496 1.664 21.504-2.048 29.888-11.52 3.136-3.584 76.096-87.168 95.36-229.312 19.52-144.256-43.776-256-46.4-260.48-11.136-19.392-33.472-23.808-49.856-10.048-16.384 13.824-20.48 40.768-9.344 60.032 1.024 2.048 49.6 89.92 34.88 198.72-14.976 111.36-72.512 178.368-73.152 179.008-14.4 16.512-15.36 43.776-1.984 60.864 5.632 7.104 12.992 11.392 20.608 12.736z";

        #endregion

        #region 节点属性

        public string Title
        {
            get => _title;
            set { SetProperty(ref _title, value); RaisePropertyChanged(nameof(InstanceInfo)); }
        }

        /// <summary>ICON</summary>
        public string Icon
        {
            get => _icon;
            set { SetProperty(ref _icon, value); }
        }

        /// <summary>所有输入连接器（分支条件 + 数据输入，Nodify 框架通过此属性显示节点上的连接器）</summary>
        public ObservableCollection<ConnectorViewModel> Inputs { get; } = new();

        /// <summary>仅数据输入连接器（非分支条件），供设置面板"输入管理"绑定</summary>
        public ObservableCollection<ConnectorViewModel> DataInputs { get; } = new();

        public int InvertInputs => Inputs.Count - DataInputs.Count;

        /// <summary>输出连接器（默认一个 "Flow" bool）</summary>
        public ObservableCollection<ConnectorViewModel> Outputs { get; } = new();

        /// <summary>输入分支 ViewModel 列表（仅分支条件，带取反标记，供设置面板绑定）</summary>
        public ObservableCollection<InputBranchViewModel> BranchInputs => _branchInputs;

        /// <summary>判断是否为分支条件（在 _invertFlags 中即为分支条件）</summary>
        private bool IsBranchCondition(ConnectorViewModel c) => _invertFlags.ContainsKey(c);

        #endregion

        #region ParentEditor

        public EditorViewModel? ParentEditor
        {
            get => _parentEditor;
            set
            {
                SetProperty(ref _parentEditor, value);
                RaisePropertyChanged(nameof(IsSubEditorOpen));
            }
        }

        public bool IsSubEditorOpen => _parentEditor != null && _parentEditor.CanNavigateBack;

        #endregion

        #region 子画布

        public EditorViewModel? SubEditorViewModel
        {
            get => _subEditorViewModel;
            set
            {
                SetProperty(ref _subEditorViewModel, value);
            }

        }

        public void OpenSubEditor()
        {
            if (_parentEditor == null)
            {
                System.Diagnostics.Debug.WriteLine("[If.OpenSubEditor] ParentEditor 未注入");
                return;
            }

            if (_subEditorViewModel == null)
                _subEditorViewModel = new EditorViewModel();

            SyncEntryExitNodes();
            RestoreSubEditorConnections();

            _parentEditor.NavigateInto(_subEditorViewModel, Title);
            RaisePropertyChanged(nameof(IsSubEditorOpen));
            RaisePropertyChanged(nameof(InstanceInfo));
        }

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

        #region 取反标记

        public bool GetInvert(ConnectorViewModel connector)
        {
            return _invertFlags.TryGetValue(connector, out var invert) && invert;
        }

        public void SetInvert(ConnectorViewModel connector, bool invert)
        {
            if (!IsBranchCondition(connector)) return;
            _invertFlags[connector] = invert;
            var wrapper = _branchInputs.FirstOrDefault(b => b.Connector == connector);
            wrapper?.SyncInvert(invert);
            RaisePropertyChanged(nameof(InstanceInfo));
        }

        #endregion

        #region 命令

        private ICommand? _addBranchCommand;
        private ICommand? _addInputCommand;
        private ICommand? _deleteInputCommand;
        private ICommand? _addOutputCommand;
        private ICommand? _deleteOutputCommand;
        private ICommand? _openSubEditorCommand;

        public ICommand AddBranchCommand => _addBranchCommand ??= new RelayCommand<object?>(_ => AddBranchCondition());
        public ICommand AddInputCommand => _addInputCommand ??= new RelayCommand<DataType?>(AddDataInput);
        public ICommand DeleteInputCommand => _deleteInputCommand ??= new RelayCommand<ConnectorViewModel?>(DeleteInput);
        public ICommand AddOutputCommand => _addOutputCommand ??= new RelayCommand<DataType?>(AddOutput);
        public ICommand DeleteOutputCommand => _deleteOutputCommand ??= new RelayCommand<ConnectorViewModel?>(DeleteOutput);
        public ICommand OpenSubEditorCommand => _openSubEditorCommand ??= new RelayCommand(OpenSubEditor);

        public bool CanDeleteInput(ConnectorViewModel? connector)
        {
            if (connector == null) return false;
            // 分支条件：IF（第一个）不可删除
            if (IsBranchCondition(connector))
            {
                var branchInputs = Inputs.Where(IsBranchCondition).ToList();
                return branchInputs.IndexOf(connector) > 0;
            }
            return true;
        }

        public bool CanDeleteOutput(ConnectorViewModel? connector)
        {
            if (connector == null || !Outputs.Contains(connector)) return false;
            return Outputs.IndexOf(connector) > 0; // 第一个（Flow）不可删除
        }

        public IfOperationViewModel()
        {
            // 默认 IF 分支条件（Bool，不可删除）
            var ifInput = new ConnectorViewModel
            {
                Title = "IF",
                DataType = DataType.Bool,
                Direction = ConnectorDirection.Input,
                IsProtect = true
            };
            ifInput.PropertyChanged += OnConnectorPropertyChanged;
            Inputs.Add(ifInput);
            _invertFlags[ifInput] = false;
            _branchInputs.Add(new InputBranchViewModel(this, ifInput, "IF", false, false));

            // 默认 Flow 输出
            var flowOutput = new ConnectorViewModel
            {
                Title = "Flow",
                DataType = DataType.Bool,
                Direction = ConnectorDirection.Output,
                IsProtect = true
            };
            flowOutput.Value = false;
            flowOutput.PropertyChanged += OnConnectorPropertyChanged;
            Outputs.Add(flowOutput);
        }

        private void RebuildBranchInputs()
        {
            _branchInputs.Clear();
            int branchIdx = 0;
            foreach (var input in Inputs)
            {
                if (!IsBranchCondition(input)) continue;
                var label = branchIdx == 0 ? "IF" : $"ELSE IF {branchIdx}";
                _branchInputs.Add(new InputBranchViewModel(this, input, label, branchIdx > 0, GetInvert(input)));
                branchIdx++;
            }
        }

        private void AddBranchCondition()
        {
            var elseIfCount = _invertFlags.Count;
            var connector = new ConnectorViewModel
            {
                Title = $"ELSE IF {elseIfCount}",
                DataType = DataType.Bool,
                Direction = ConnectorDirection.Input
            };
            connector.PropertyChanged += OnConnectorPropertyChanged;
            Inputs.Insert(elseIfCount,connector);
            _invertFlags[connector] = false;
            RebuildBranchInputs();

            if (_subEditorViewModel != null)
                SyncEntryExitNodes();

            RaisePropertyChanged(nameof(InstanceInfo));
        }

        private void AddDataInput(DataType? dataType)
        {
            var dt = dataType ?? DataType.String;
            if (dt == DataType.Any) return;

            var connector = new ConnectorViewModel
            {
                Title = $"输入{DataInputs.Count + 1}",
                DataType = dt,
                Direction = ConnectorDirection.Input
            };
            connector.PropertyChanged += OnConnectorPropertyChanged;
            // 插入到最后一个分支条件之后（分支条件始终在数据输入上方）
            var branchCount = _invertFlags.Count;
            Inputs.Insert(branchCount, connector);
            DataInputs.Add(connector);

            if (_subEditorViewModel != null)
                SyncEntryExitNodes();

            RaisePropertyChanged(nameof(InstanceInfo));
        }

        /// <summary>
        /// 删除输入：自动判断是分支条件还是数据输入
        /// </summary>
        private void DeleteInput(ConnectorViewModel? connector)
        {
            if (connector == null || !Inputs.Contains(connector)) return;

            if (IsBranchCondition(connector))
            {
                // IF（第一个分支条件）不可删除
                var branchList = Inputs.Where(IsBranchCondition).ToList();
                if (branchList.Count > 0 && branchList[0] == connector) return;

                DisconnectConnector(connector);
                connector.PropertyChanged -= OnConnectorPropertyChanged;
                Inputs.Remove(connector);
                _invertFlags.Remove(connector);
                RebuildBranchInputs();

                if (_subEditorViewModel != null)
                    SyncEntryExitNodes();
                RaisePropertyChanged(nameof(InstanceInfo));
                return;
            }

            // 数据输入删除
            DisconnectConnector(connector);
            connector.PropertyChanged -= OnConnectorPropertyChanged;
            Inputs.Remove(connector);
            DataInputs.Remove(connector);

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
                Title = $"输出{Outputs.Count}",
                DataType = dt,
                Direction = ConnectorDirection.Output
            };
            connector.PropertyChanged += OnConnectorPropertyChanged;
            Outputs.Add(connector);

            if (_subEditorViewModel != null)
                SyncEntryExitNodes();

            RaisePropertyChanged(nameof(InstanceInfo));
        }

        private void DeleteOutput(ConnectorViewModel? connector)
        {
            if (connector == null || !Outputs.Contains(connector)) return;
            if (Outputs.IndexOf(connector) == 0) return; // Flow 不可删除

            connector.PropertyChanged -= OnConnectorPropertyChanged;
            DisconnectConnector(connector);
            Outputs.Remove(connector);

            if (_subEditorViewModel != null)
                SyncEntryExitNodes();

            RaisePropertyChanged(nameof(InstanceInfo));
        }

        private void OnConnectorPropertyChanged(object? sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName != nameof(ConnectorViewModel.Title)) return;
            if (sender is not ConnectorViewModel connector) return;
            if (_subEditorViewModel == null) return;

            var entryNodes = _subEditorViewModel.Operations
                .Where(n => n.Instance is IfEntryNode)
                .ToList();

            // 输入 → 入口节点
            var inputIdx = Inputs.IndexOf(connector);
            if (inputIdx >= 0 && inputIdx < entryNodes.Count
                && entryNodes[inputIdx].Instance is IfEntryNode entry)
            {
                entry.NodeName = connector.Title;
                return;
            }

            // 输出 → 出口节点
            var exitNodes = _subEditorViewModel.Operations
                .Where(n => n.Instance is IfExitNode)
                .ToList();
            var outputIdx = Outputs.IndexOf(connector);
            if (outputIdx >= 0 && outputIdx < exitNodes.Count
                && exitNodes[outputIdx].Instance is IfExitNode exit)
            {
                exit.NodeName = connector.Title;
            }
        }

        #endregion

        #region 子画布入口/出口节点同步

        /// <summary>
        /// 子画布入口节点顺序：先分支条件（IF/ELSE IF），再数据输入
        /// </summary>
        private void SyncEntryExitNodes()
        {
            if (_subEditorViewModel == null) return;

            // 收集所有需要的入口名称（分支条件在前，数据输入在后）
            var allEntryNames = new List<string>();
            allEntryNames.AddRange(Inputs.Where(IsBranchCondition).Select(b => b.Title));
            allEntryNames.AddRange(DataInputs.Select(i => i.Title));
            var neededEntryNames = new HashSet<string>(allEntryNames);

            // 现有入口节点
            var existingEntries = _subEditorViewModel.Operations
                .Where(n => n.Instance is IfEntryNode)
                .ToDictionary(n => ((IfEntryNode)n.Instance).NodeName);

            // 移除多余的
            foreach (var kv in existingEntries)
            {
                if (!neededEntryNames.Contains(kv.Key))
                    _subEditorViewModel.Operations.Remove(kv.Value);
            }

            // 按顺序重建入口节点（先分支条件，再数据输入）
            double x = 100, y = 100;
            foreach (var name in allEntryNames)
            {
                var connector = Inputs.FirstOrDefault(i => i.Title == name);
                var isBranch = connector != null && IsBranchCondition(connector);
                var dataType = isBranch ? DataType.Bool : (connector?.DataType ?? DataType.String);

                if (!existingEntries.ContainsKey(name))
                {
                    var entry = new IfEntryNode { NodeName = name, DataType = dataType };
                    var nodeVm = new OperationNodeViewModel(entry);
                    nodeVm.Location = new Point(x, y);
                    nodeVm.ExecuteCommand = _subEditorViewModel.ExecuteCommand;
                    _subEditorViewModel.Operations.Add(nodeVm);
                }
                else
                {
                    var existing = (IfEntryNode)existingEntries[name].Instance;
                    existing.DataType = dataType;
                }
                y += 120;
            }

            // 出口节点
            var existingExits = _subEditorViewModel.Operations
                .Where(n => n.Instance is IfExitNode)
                .ToDictionary(n => ((IfExitNode)n.Instance).NodeName);
            var neededExitNames = new HashSet<string>(Outputs.Select(o => o.Title));

            foreach (var kv in existingExits)
            {
                if (!neededExitNames.Contains(kv.Key))
                    _subEditorViewModel.Operations.Remove(kv.Value);
            }

            x = 800; y = 100;
            foreach (var output in Outputs)
            {
                if (!existingExits.ContainsKey(output.Title))
                {
                    var exit = new IfExitNode { NodeName = output.Title, DataType = output.DataType };
                    var nodeVm = new OperationNodeViewModel(exit);
                    nodeVm.Location = new Point(x, y);
                    nodeVm.ExecuteCommand = _subEditorViewModel.ExecuteCommand;
                    _subEditorViewModel.Operations.Add(nodeVm);
                }
                else
                {
                    var existing = (IfExitNode)existingExits[output.Title].Instance;
                    existing.DataType = output.DataType;
                }
                y += 120;
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


        private void RunStatusMessage(NodeRunStatus status, string smessage)
        {
            Application.Current?.Dispatcher.BeginInvoke(() =>
            {
                _runStatus = status;
                StatusMessage = smessage;
            });
        }

        public bool Execute()
        {
            if (Application.Current.Dispatcher.CheckAccess())
                return ExecuteInternal();
            else
                return Application.Current.Dispatcher.Invoke(() => ExecuteInternal());
        }

        private bool ExecuteInternal()
        {
            RunStatusMessage(NodeRunStatus.Disabled, string.Empty);
            try
            {
                if (_subEditorViewModel == null || _subEditorViewModel.Operations.Count == 0)
                {
                    var hasMatch = EvaluateConditions(out _);
                    if (Outputs.Count > 0)
                        Outputs[0].Value = hasMatch;
                    RunStatusMessage(NodeRunStatus.Error, hasMatch ? "IF 匹配" : "无匹配分支");
                    return hasMatch;
                }

                SyncEntryExitNodes();
                RestoreSubEditorConnections();

                // 评估所有分支条件，找到第一个匹配的
                var matched = EvaluateConditions(out var matchedBranchIdx);

                // 获取入口/出口节点
                var entryNodes = _subEditorViewModel.Operations
                    .Where(n => n.Instance is IfEntryNode)
                    .ToList();
                var exitNodes = _subEditorViewModel.Operations
                    .Where(n => n.Instance is IfExitNode)
                    .ToList();

                // 分支条件按 Inputs 中顺序，数据输入按 DataInputs 顺序
                var branchInputs = Inputs.Where(IsBranchCondition).ToList();

                // 设置分支条件入口节点值：匹配分支为 true，其余为 false
                for (int i = 0; i < Math.Min(branchInputs.Count, entryNodes.Count); i++)
                {
                    if (entryNodes[i].Outputs.Count > 0)
                        entryNodes[i].Outputs[0].Value = matched && i == matchedBranchIdx;
                    if (entryNodes[i].Instance is IfEntryNode entry)
                        entry.NotifyValueChanged();
                }

                // 设置数据输入入口节点值（在分支条件入口之后）
                for (int i = 0; i < DataInputs.Count; i++)
                {
                    var entryIdx = branchInputs.Count + i;
                    if (entryIdx < entryNodes.Count && entryNodes[entryIdx].Outputs.Count > 0)
                        entryNodes[entryIdx].Outputs[0].Value = DataInputs[i].Value;
                    if (entryIdx < entryNodes.Count && entryNodes[entryIdx].Instance is IfEntryNode entry)
                        entry.NotifyValueChanged();
                }

                if (!matched)
                {
                    if (Outputs.Count > 0)
                        Outputs[0].Value = false;
                    RunStatusMessage(NodeRunStatus.Error, "无匹配分支");
                    return false;
                }

                // 拓扑排序执行子画布节点
                var allNodes = _subEditorViewModel.Operations
                    .Where(n => n.Instance is not IfEntryNode && n.Instance is not IfExitNode)
                    .ToList();

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

                var queue = new Queue<OperationNodeViewModel>();
                foreach (var node in allNodes)
                    if (inDegree[node] == 0)
                        queue.Enqueue(node);

                // 物化为字典避免在循环中枚举 ObservableCollection（防止并发修改导致卡死）
                var connDict = _subEditorViewModel.Connections
                    .Where(c => c.Input != null)
                    .GroupBy(c => c.Input!)
                    .ToDictionary(g => g.Key, g => g.First().Output);

                while (queue.Count > 0)
                {
                    var node = queue.Dequeue();

                    for (int i = 0; i < node.Inputs.Count; i++)
                    {
                        var input = node.Inputs[i];
                        if (input.IsConnected && connDict.TryGetValue(input, out var sourceOutput))
                        {
                            input.Value = sourceOutput?.Value;
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

                // 从出口节点读取值到输出
                var exitConnDict = _subEditorViewModel.Connections
                    .Where(c => c.Input != null)
                    .GroupBy(c => c.Input!)
                    .ToDictionary(g => g.Key, g => g.First().Output);

                for (int i = 0; i < Math.Min(Outputs.Count, exitNodes.Count); i++)
                {
                    if (exitNodes[i].Inputs.Count > 0)
                    {
                        var exitInput = exitNodes[i].Inputs[0];
                        if (exitConnDict.TryGetValue(exitInput, out var sourceOutput))
                        {
                            exitInput.Value = sourceOutput?.Value;
                        }
                        Outputs[i].Value = exitInput.Value;
                    }
                    if (exitNodes[i].Instance is IfExitNode exit)
                        exit.NotifyValueChanged();
                }

                // Flow 输出设为 true
                //if (Outputs.Count > 0)
                //    Outputs[0].Value = true;

                RunStatusMessage(NodeRunStatus.Completed, "执行成功");
                return true;
            }
            catch (Exception ex)
            {
                RunStatusMessage(NodeRunStatus.Error, $"异常: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// 评估所有分支条件，返回是否匹配及匹配的分支索引
        /// </summary>
        private bool EvaluateConditions(out int matchedIndex)
        {
            matchedIndex = -1;
            int branchIdx = 0;
            foreach (var input in Inputs)
            {
                if (!IsBranchCondition(input)) continue;

                var condition = input.Value is bool b ? b : false;
                if (GetInvert(input))
                    condition = !condition;

                if (condition)
                {
                    matchedIndex = branchIdx;
                    return true;
                }
                branchIdx++;
            }
            return false;
        }

        #endregion

        #region ISerializableOperation

        private const int SerializeVersion = 4;

        private List<PendingConnectionInfo>? _pendingConnectionData;

        private class PendingConnectionInfo
        {
            public byte SourceType;           // 0=entry node, 1=regular node
            public string? SourceEntryName;   // entry node name (if SourceType==0)
            public int SourceNodeIndex;       // regular node index (if SourceType==1)
            public int SourceConnectorIndex;

            public byte TargetType;           // 0=exit node, 1=regular node
            public string? TargetExitName;    // exit node name (if TargetType==0)
            public int TargetNodeIndex;       // regular node index (if TargetType==1)
            public int TargetConnectorIndex;
        }

        public void Serialize(BinaryWriter writer)
        {
            writer.Write(SerializeVersion);
            writer.Write(Title);

            // 所有输入（含分支条件 + 数据输入），每个写 IsBranch 标记
            writer.Write(Inputs.Count);
            foreach (var input in Inputs)
            {
                writer.Write(input.Title);
                writer.Write((int)input.DataType);
                writer.Write(input.IsProtect);
                var isBranch = IsBranchCondition(input);
                writer.Write(isBranch);
                if (isBranch)
                    writer.Write(GetInvert(input));
            }

            // 输出
            writer.Write(Outputs.Count);
            foreach (var output in Outputs)
            {
                writer.Write(output.Title);
                writer.Write((int)output.DataType);
                writer.Write(output.IsProtect);
            }

            // 子画布
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
            DataInputs.Clear();
            _invertFlags.Clear();

            if (version >= 4)
            {
                // V4+：统一 Inputs，每个带 IsBranch 标记
                var inputCount = reader.ReadInt32();
                for (int i = 0; i < inputCount; i++)
                {
                    var connector = new ConnectorViewModel
                    {
                        Title = reader.ReadString(),
                        DataType = (DataType)reader.ReadInt32(),
                        Direction = ConnectorDirection.Input,
                        IsProtect = reader.ReadBoolean()
                    };
                    connector.PropertyChanged += OnConnectorPropertyChanged;
                    Inputs.Add(connector);

                    var isBranch = reader.ReadBoolean();
                    if (isBranch)
                        _invertFlags[connector] = reader.ReadBoolean();
                    else
                        DataInputs.Add(connector);
                }
            }
            else if (version >= 3)
            {
                // V3：旧格式 BranchConditions + Inputs 分离
                var branchCount = reader.ReadInt32();
                for (int i = 0; i < branchCount; i++)
                {
                    var connector = new ConnectorViewModel
                    {
                        Title = reader.ReadString(),
                        DataType = (DataType)reader.ReadInt32(),
                        Direction = ConnectorDirection.Input,
                        IsProtect = reader.ReadBoolean()
                    };
                    connector.PropertyChanged += OnConnectorPropertyChanged;
                    Inputs.Add(connector);
                    _invertFlags[connector] = reader.ReadBoolean();
                }

                var inputCount = reader.ReadInt32();
                for (int i = 0; i < inputCount; i++)
                {
                    var connector = new ConnectorViewModel
                    {
                        Title = reader.ReadString(),
                        DataType = (DataType)reader.ReadInt32(),
                        Direction = ConnectorDirection.Input,
                        IsProtect = reader.ReadBoolean()
                    };
                    connector.PropertyChanged += OnConnectorPropertyChanged;
                    Inputs.Add(connector);
                    DataInputs.Add(connector);
                }
            }
            else
            {
                // V1/V2：旧格式所有输入都是分支条件
                var inputCount = reader.ReadInt32();
                for (int i = 0; i < inputCount; i++)
                {
                    var connector = new ConnectorViewModel
                    {
                        Title = reader.ReadString(),
                        DataType = (DataType)reader.ReadInt32(),
                        Direction = ConnectorDirection.Input,
                        IsProtect = reader.ReadBoolean()
                    };
                    connector.PropertyChanged += OnConnectorPropertyChanged;
                    Inputs.Add(connector);

                    var invert = version >= 2 ? reader.ReadBoolean() : false;
                    _invertFlags[connector] = invert;
                }
            }

            // 输出
            Outputs.Clear();
            var outputCount = reader.ReadInt32();
            for (int i = 0; i < outputCount; i++)
            {
                var connector = new ConnectorViewModel
                {
                    Title = reader.ReadString(),
                    DataType = (DataType)reader.ReadInt32(),
                    Direction = ConnectorDirection.Output,
                    IsProtect = reader.ReadBoolean()
                };
                connector.PropertyChanged += OnConnectorPropertyChanged;
                Outputs.Add(connector);
            }

            var hasSubEditor = reader.ReadBoolean();
            if (hasSubEditor)
                DeserializeSubEditor(reader, version);

            RebuildBranchInputs();
            RaisePropertyChanged(nameof(InstanceInfo));
        }

        private void SerializeSubEditor(BinaryWriter writer)
        {
            if (_subEditorViewModel == null) return;

            var nodes = _subEditorViewModel.Operations
                .Where(n => n.Instance is not IfEntryNode && n.Instance is not IfExitNode)
                .ToList();
            var entryNodes = _subEditorViewModel.Operations
                .Where(n => n.Instance is IfEntryNode)
                .ToList();
            var exitNodes = _subEditorViewModel.Operations
                .Where(n => n.Instance is IfExitNode)
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

            var conns = _subEditorViewModel.Connections
                .Where(c => c.Output != null && c.Input != null)
                .ToList();
            writer.Write(conns.Count);
            foreach (var conn in conns)
            {
                var sourceEntry = entryNodes.FirstOrDefault(n => n.Outputs.Contains(conn.Output!));
                if (sourceEntry != null)
                {
                    writer.Write((byte)0);
                    writer.Write(((IfEntryNode)sourceEntry.Instance).NodeName);
                    writer.Write(0);
                }
                else
                {
                    var sourceNode = nodes.FirstOrDefault(n => n.Outputs.Contains(conn.Output!));
                    writer.Write((byte)1);
                    writer.Write(sourceNode != null ? nodes.IndexOf(sourceNode) : -1);
                    writer.Write(sourceNode != null ? sourceNode.Outputs.IndexOf(conn.Output!) : -1);
                }

                var targetExit = exitNodes.FirstOrDefault(n => n.Inputs.Contains(conn.Input!));
                if (targetExit != null)
                {
                    writer.Write((byte)0);
                    writer.Write(((IfExitNode)targetExit.Instance).NodeName);
                    writer.Write(0);
                }
                else
                {
                    var targetNode = nodes.FirstOrDefault(n => n.Inputs.Contains(conn.Input!));
                    writer.Write((byte)1);
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
                var assemblyName = reader.ReadString();
                var fullName = reader.ReadString();
                var title = reader.ReadString();
                var locX = reader.ReadDouble();
                var locY = reader.ReadDouble();

                var type = Type.GetType(assemblyName) ?? Type.GetType(fullName);
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

                        nodeVm.ExecuteCommand = _subEditorViewModel.ExecuteCommand;
                        if (instance is IConnectionAware connectionAware)
                            connectionAware.Connections = _subEditorViewModel.Connections;

                        _subEditorViewModel.Operations.Add(nodeVm);
                    }
                }
            }

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

        private void RestoreSubEditorConnections()
        {
            if (_subEditorViewModel == null || _pendingConnectionData == null
                || _pendingConnectionData.Count == 0)
                return;

            var nodes = _subEditorViewModel.Operations
                .Where(n => n.Instance is not IfEntryNode && n.Instance is not IfExitNode)
                .ToList();
            var entryNodes = _subEditorViewModel.Operations
                .Where(n => n.Instance is IfEntryNode)
                .ToDictionary(n => ((IfEntryNode)n.Instance).NodeName);
            var exitNodes = _subEditorViewModel.Operations
                .Where(n => n.Instance is IfExitNode)
                .ToDictionary(n => ((IfExitNode)n.Instance).NodeName);

            foreach (var info in _pendingConnectionData)
            {
                ConnectorViewModel? source = null;
                ConnectorViewModel? target = null;

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
                    _subEditorViewModel.Connections.Add(new ConnectionViewModel(target, source));
                }
            }

            _pendingConnectionData = null;
        }

        #endregion

        #region 显示属性

        public string InstanceInfo
        {
            get
            {
                var info = $"IF [{Title}]";
                var branches = Inputs.Where(IsBranchCondition).ToList();
                if (branches.Count > 0)
                {
                    var branchLabels = branches.Select(inp =>
                    {
                        var invert = GetInvert(inp) ? "!" : "";
                        return $"{invert}{inp.Title}";
                    });
                    info += $"\n分支: {string.Join(" → ", branchLabels)}";
                }
                if (DataInputs.Count > 0)
                    info += $"\n入: {string.Join(", ", DataInputs.Select(i => FormatConnectorInfo(i)))}";
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
    /// IF 子画布入口节点（分支条件）
    /// </summary>
    public class IfEntryNode : INotifyPropertyChanged
    {
        private string _nodeName = "入口";
        private DataType _dataType = DataType.Bool;

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

        public object? Value => Outputs.Count > 0 ? Outputs[0].Value : null;

        public string DisplayInfo => Value != null
            ? $"▶ {NodeName}\n({DataType}) = {Value}"
            : $"▶ {NodeName}\n({DataType})";

        public ObservableCollection<ConnectorViewModel> Inputs { get; } = new();

        public ObservableCollection<ConnectorViewModel> Outputs { get; } = new()
        {
            new ConnectorViewModel
            {
                Title = "输出",
                DataType = DataType.Bool,
                Direction = ConnectorDirection.Output
            }
        };

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
    /// IF 子画布出口节点（数据输出）
    /// </summary>
    public class IfExitNode : INotifyPropertyChanged
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

        public object? Value => Inputs.Count > 0 ? Inputs[0].Value : null;

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