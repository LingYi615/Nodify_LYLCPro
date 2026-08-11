using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Unicode;
using System.Windows;
using System.Windows.Input;
using LYCorePro.Common.Helper;
using LYCorePro.Core;
using Newtonsoft.Json.Linq;
using Nodify;
using PluginBase.Interfaces;
using PluginBase.Models;

namespace DataCollectionPlugin
{
    /// <summary>
    /// 数据集合操作（节点实例）
    /// - 多个输入，名称可变更
    /// - 一个输出，数据类型可选 Array 或 Json（String）
    /// - 勾选"数据类型一致"时生成数组，不一致时生成 JSON
    /// </summary>
    public class DataCollectionOperationViewModel : NotifyPropertyBase, IExecutableOperation, ISerializableOperation, IConnectionAware
    {
        #region 静态资源

        public static DataType[] AvailableDataTypes { get; } = Enum
            .GetValues(typeof(DataType))
            .Cast<DataType>()
            .Where(dt =>dt != DataType.Any)
            .ToArray();

        public static DataType[] OutputTypeOptions { get; } = new[]
        {
            DataType.Array,
            DataType.Json
        };

        #endregion

        #region 字段

        private string _title = "数据集合";
        private string? _statusMessage;
        private NodeRunStatus _runStatus;
        private bool _requireSameType = true;
        private DataType _outputType = DataType.Array;
        // IConnectionAware 实现
        private ObservableCollection<ConnectionViewModel>? _connections;


        private string _icon = "M143.744 510.08a2.688 2.688 0 0 0 0 3.84l271.552 271.488a2.688 2.688 0 0 0 3.776 0l241.344-241.344a2.688 2.688 0 0 0 0-3.84L388.928 268.8a2.688 2.688 0 0 0-3.84 0l-241.28 241.408zm-56.512 60.352a82.688 82.688 0 0 1 0-116.864l241.28-241.408a82.688 82.688 0 0 1 116.928 0l271.552 271.552a82.688 82.688 0 0 1 0 116.928l-241.344 241.344a82.688 82.688 0 0 1-116.928 0L87.232 570.432zM399.744 510.08a2.688 2.688 0 0 0 0 3.84l241.408 241.28a2.688 2.688 0 0 0 3.776 0l241.28-241.28a2.688 2.688 0 0 0 0-3.84l-241.28-241.28a2.688 2.688 0 0 0-3.84 0l-241.28 241.28zm-56.512 60.352a82.688 82.688 0 0 1 0-116.864l241.28-241.408a82.688 82.688 0 0 1 116.992 0l241.28 241.408a82.688 82.688 0 0 1 0 116.864l-241.28 241.408a82.688 82.688 0 0 1-116.928 0L343.232 570.432z";

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

        public ObservableCollection<ConnectorViewModel> Inputs { get; } = new();
        public ObservableCollection<ConnectorViewModel> Outputs { get; } = new();

        // 新增：安全获取第一个输出值，XAML绑定专用，杜绝[0]下标语法
        public object? FirstOutputValue
        {
            get
            {
                if (Outputs.Count == 0) return null;
                return Outputs[0].Value;
            }
        }

        /// <summary>是否要求数据类型一致</summary>
        public bool RequireSameType
        {
            get => _requireSameType;
            set
            {
                if (_requireSameType != value)
                {
                    SetProperty(ref _requireSameType, value);
                    RaisePropertyChanged(nameof(InstanceInfo));
                    UpdateOutputDataType();
                }
            }
        }

        /// <summary>输出类型</summary>
        public DataType OutputType
        {
            get => _outputType;
            set
            {
                if (_outputType != value)
                {
                    SetProperty(ref _outputType ,value);
                    RaisePropertyChanged(nameof(InstanceInfo));
                    UpdateOutputDataType();
                }
            }
        }

        #endregion

        #region 命令

        private ICommand? _addInputCommand;
        private ICommand? _deleteInputCommand;

        public ICommand AddInputCommand => _addInputCommand ??= new RelayCommand<DataType?>(AddInput);
        public ICommand DeleteInputCommand => _deleteInputCommand ??= new RelayCommand<ConnectorViewModel?>(DeleteInput);

        public DataCollectionOperationViewModel()
        {
            // 默认创建两个输入
            AddInput(DataType.String);
            AddInput(DataType.String);

            // 创建默认输出（Array 类型）
            var output = new ConnectorViewModel
            {
                Title = "集合",
                DataType = DataType.String, // 默认 String（JSON 时用）
                Direction = ConnectorDirection.Output
            };
            Outputs.Add(output);
            // 监听输出集合变化，刷新FirstOutputValue
            Outputs.CollectionChanged += OnOutputCollectionChanged;
            UpdateOutputDataType();
        }

        // 输出集合变更触发派生属性刷新
        private void OnOutputCollectionChanged(object? sender, System.Collections.Specialized.NotifyCollectionChangedEventArgs e)
        {
            RaisePropertyChanged(nameof(FirstOutputValue));
        }

        private void AddInput(DataType? dataType)
        {
            var dt = dataType ?? DataType.String;
            if (dt == DataType.TCPAgreement || dt == DataType.Any) return;

            var connector = new ConnectorViewModel
            {
                Title = $"输入{Inputs.Count + 1}",
                DataType = dt,
                Direction = ConnectorDirection.Input
            };
            connector.PropertyChanged += OnConnectorPropertyChanged;
            Inputs.Add(connector);

            RaisePropertyChanged(nameof(InstanceInfo));
        }

        private void DeleteInput(ConnectorViewModel? connector)
        {
            if (connector == null || !Inputs.Contains(connector)) return;
            DisconnectConnector(connector); 
            connector.PropertyChanged -= OnConnectorPropertyChanged;
            Inputs.Remove(connector);

            RaisePropertyChanged(nameof(InstanceInfo));
        }

        private void OnConnectorPropertyChanged(object? sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(ConnectorViewModel.Title))
                RaisePropertyChanged(nameof(InstanceInfo));
        }

        /// <summary>
        /// 根据输出类型更新输出连接器的 DataType
        /// </summary>
        private void UpdateOutputDataType()
        {
            if (Outputs.Count == 0) return;

            if (_outputType == DataType.Json)
            {
                // JSON 输出为 String 类型
                Outputs[0].DataType = DataType.String;
            }
            else
            {
                // Array 输出：如果要求类型一致，使用第一个输入的类型；否则使用 String
                if (_requireSameType && Inputs.Count > 0)
                {
                    Outputs[0].DataType = Inputs[0].DataType;
                }
                else
                {
                    Outputs[0].DataType = DataType.String;
                }
            }
            // 更新输出类型后刷新显示值
            RaisePropertyChanged(nameof(FirstOutputValue));
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
            RunStatusMessage(NodeRunStatus.Disabled, string.Empty);
            try
            {
                if (Inputs.Count == 0)
                {
                    RunStatusMessage(NodeRunStatus.Error, "无输入数据");
                    return false;
                }

                // 1. 构造【输入名称-值】字典，用于JSON输出带title
                var inputDict = new Dictionary<string, object?>();
                var valueList = new List<object?>();
                foreach (var input in Inputs)
                {
                    // 处理重复输入名自动后缀区分
                    string key = input.Title;
                    inputDict[key] = input.Value;
                    valueList.Add(input.Value);
                }
                var options = new JsonSerializerOptions
                {
                    Encoder = JavaScriptEncoder.Create(UnicodeRanges.All),
                    WriteIndented = true
                };
                if (_requireSameType)
                {
                    // 检查所有输入数据类型是否一致
                    var firstType = Inputs.Count > 0 ? Inputs[0].DataType : DataType.String;
                    var allSameType = Inputs.All(i => i.DataType == firstType);

                    if (!allSameType)
                    {
                        RunStatusMessage(NodeRunStatus.Error, "输入数据类型不一致");
                        return false;
                    }

                    if (_outputType == DataType.Array)
                    {
                        // Array模式：输出真实List<object>，不再拼接字符串
                        Outputs[0].Value = string.Join("," ,valueList.Select(x=>x.ToString()));
                    }
                    else
                    {
                        // JSON模式：序列化带输入Title的字典
                        var json = JsonSerializer.Serialize(inputDict, options);
                        Outputs[0].Value = json;
                    }
                }
                else
                {
                    // 不要求类型一致，统一输出带标题JSON
                    var json = JsonSerializer.Serialize(inputDict, options);
                    Outputs[0].Value = json;
                }
                // 执行完成刷新输出值显示
                RaisePropertyChanged(nameof(FirstOutputValue));
                StatusMessage = "执行成功";
                RunStatus = NodeRunStatus.Completed;
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

        private const int SerializeVersion = 1;

        public void Serialize(BinaryWriter writer)
        {
            writer.Write(SerializeVersion);
            writer.Write(Title);
            writer.Write(_requireSameType);
            writer.Write((int)_outputType);

            // 输入
            writer.Write(Inputs.Count);
            foreach (var input in Inputs)
            {
                writer.Write(input.Title);
                writer.Write((int)input.DataType);
            }
        }

        public void Deserialize(BinaryReader reader)
        {
            var version = reader.ReadInt32();
            Title = reader.ReadString();
            _requireSameType = reader.ReadBoolean();
            _outputType = (DataType)reader.ReadInt32();

            Inputs.Clear();
            var inputCount = reader.ReadInt32();
            for (int i = 0; i < inputCount; i++)
            {
                var connector = new ConnectorViewModel
                {
                    Title = reader.ReadString(),
                    DataType = (DataType)reader.ReadInt32(),
                    Direction = ConnectorDirection.Input
                };
                connector.PropertyChanged += OnConnectorPropertyChanged;
                Inputs.Add(connector);
            }

            // 确保输出存在
            if (Outputs.Count == 0)
            {
                Outputs.Add(new ConnectorViewModel
                {
                    Title = "集合",
                    DataType = DataType.String,
                    Direction = ConnectorDirection.Output
                });
                Outputs.CollectionChanged += OnOutputCollectionChanged;
            }
            UpdateOutputDataType();

            RaisePropertyChanged(nameof(RequireSameType));
            RaisePropertyChanged(nameof(OutputType));
            RaisePropertyChanged(nameof(InstanceInfo));
            RaisePropertyChanged(nameof(FirstOutputValue));
        }

        #endregion

        #region 显示属性

        public string InstanceInfo
        {
            get
            {
                var info = $"数据集合 [{(_requireSameType ? "类型一致" : "自动收集")}]";
                info += $"\n模式: {(_outputType == DataType.Array ? "Array" : "JSON")}";
                info += $"\n入: {string.Join(", ", Inputs.Select(i => $"{i.Title}({i.DataType})"))}";
                if (!string.IsNullOrEmpty(StatusMessage))
                    info += $"\n{StatusMessage}";
                return info;
            }
        }

        #endregion

        #region IConnectionAware 实现

        /// <summary>
        /// 编辑器中所有连线的引用（由编辑器在节点创建时注入）
        /// </summary>
        public ObservableCollection<ConnectionViewModel> Connections
        {
            get => _connections ??= new ObservableCollection<ConnectionViewModel>();
            set => _connections = value;
        }

        /// <summary>
        /// 断开指定连接器上的所有连线
        /// </summary>
        public void DisconnectConnector(ConnectorViewModel connector)
        {
            if (_connections == null || connector == null) return;

            // 查找该连接器涉及的所有连线（作为输入或输出）
            var toRemove = _connections.Where(c =>
                c.Input == connector || c.Output == connector).ToList();

            if (toRemove.Count == 0) return;

            foreach (var conn in toRemove)
            {
                // 断开连线时更新 IsConnected 状态
                // 仅在连接器没有其他连线时才设为 false
                if (conn.Output != null && !_connections.Any(c => c != conn && c.Output == conn.Output))
                    conn.Output.IsConnected = false;
                if (conn.Input != null && !_connections.Any(c => c != conn && c.Input == conn.Input))
                    conn.Input.IsConnected = false;
                _connections.Remove(conn);
            }

            System.Diagnostics.Debug.WriteLine(
                $"[DataCollectionOperationViewModel] 已断开 '{connector.Title}' 上的 {toRemove.Count} 条连线");
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
}