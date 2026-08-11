using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using System.Windows.Threading;
using LYCorePro.Common.Helper;
using LYCorePro.Communacation;
using LYCorePro.Core;
using Nodify;
using OfficeOpenXml;
using PluginBase.Interfaces;
using PluginBase.Models;

namespace TcpsWritePlugin
{
    /// <summary>
    /// TCP 写入插件操作（节点实例）
    /// 输入：TCPAgreement（ICommunication 通讯实例）+ 动态写入变量
    /// 输出：outflag（可选）
    /// 实现 IExecutableOperation 接口，点击执行按钮时自动调用 Execute()
    /// </summary>
    public class TcpsWriteOperationViewModel : NotifyPropertyBase, IExecutableOperation, ISerializableOperation, IConnectionAware
    {
        #region 字段

        private string _title = "TCP 写入";
        private string? _statusMessage;
        private NodeRunStatus _runStatus;

        private ICommunication? _communicationInstance;
        private bool _isConnected;

        private string _icon = "M511.472588 1023.999607a511.240218 511.240218 0 1 1 499.442367-401.520203 39.326171 39.326171 0 0 1-46.798143 29.88789 39.326171 39.326171 0 0 1-29.88789-46.798143A432.587877 432.587877 0 1 0 511.472588 945.347265a443.599205 443.599205 0 0 0 91.236716-9.438281 39.326171 39.326171 0 0 1 46.798143 29.88789 39.326171 39.326171 0 0 1-30.281152 46.798143A522.251546 522.251546 0 0 1 511.472588 1023.999607z M39.55854 515.905482a39.326171 39.326171 0 0 1-28.314843-66.85449c117.978512-121.124606 311.856533-193.48476 520.678499-193.48476a766.467066 766.467066 0 0 1 476.239927 152.585542 39.326171 39.326171 0 0 1-49.550975 61.348827 683.882108 683.882108 0 0 0-426.688952-135.282027c-187.979096 0-361.407508 63.315135-464.048813 169.495795a39.326171 39.326171 0 0 1-28.314843 12.191113z M511.472588 1023.999607a39.326171 39.326171 0 0 1-27.52832-11.011328c-121.124606-117.978512-193.48476-311.856533-193.484759-520.678499A764.500757 764.500757 0 0 1 443.438312 16.069853a39.326171 39.326171 0 1 1 60.955565 49.550975 686.241678 686.241678 0 0 0-135.282027 426.688952c0 187.979096 63.708396 361.407508 169.889057 464.048813A39.326171 39.326171 0 0 1 511.472588 1023.999607z M693.159496 515.905482a39.326171 39.326171 0 0 1-39.326171-39.326171 637.870488 637.870488 0 0 0-136.06855-410.171959 39.326171 39.326171 0 0 1 4.71914-55.449901 39.326171 39.326171 0 0 1 55.056639 4.325879 719.275661 719.275661 0 0 1 154.945113 461.295981 39.326171 39.326171 0 0 1-39.326171 39.326171zM511.472588 697.199129c-199.383685 0-385.396472-57.809471-497.476059-154.551851a39.326171 39.326171 0 1 1 51.124022-59.775779C163.435977 567.816027 330.572203 618.546787 511.472588 618.546787a39.326171 39.326171 0 0 1 0 78.652342zM747.429611 987.81953h-3.539355a39.326171 39.326171 0 0 1-34.60703-29.494628L617.653248 595.344347a39.326171 39.326171 0 0 1 12.977637-39.326171 39.326171 39.326171 0 0 1 41.685741-5.505664l326.800478 152.192281a39.326171 39.326171 0 0 1-4.325879 72.753415l-149.83271 47.977928-60.955565 140.787691a39.326171 39.326171 0 0 1-36.573339 23.595703z m-33.427245-331.91288L758.047678 827.368754l22.022655-51.124022a39.326171 39.326171 0 0 1 23.988964-21.629394l71.966892-23.202441z";


        // inflag / outflag 开关
        private bool _isInFlagEnabled;
        private bool _isOutFlagEnabled;
        private DataType _inFlagDataType = DataType.Bool;
        private DataType _outFlagDataType = DataType.Bool;

        // IConnectionAware 实现
        private ObservableCollection<ConnectionViewModel>? _connections;

        // 变量列表（用户可动态增删）
        private readonly ObservableCollection<WriteVariable> _writeVariables = new();

        #endregion

        #region 命令

        public ICommand AddVariableCommand { get; }
        public ICommand RemoveVariableCommand { get; }
        public ICommand ImportVariablesCommand { get; }
        public ICommand ExportVariablesCommand { get; }

        public TcpsWriteOperationViewModel()
        {
            AddVariableCommand = new RelayCommand<object?>(_ => AddVariable());
            RemoveVariableCommand = new RelayCommand<WriteVariable>(RemoveVariable);
            ImportVariablesCommand = new RelayCommand<object?>(_ => ImportVariables());
            ExportVariablesCommand = new RelayCommand<object?>(_ => ExportVariables());
        }

        #endregion

        #region 节点属性（供 OperationNodeViewModel 反射读取）

        /// <summary>节点标题</summary>
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

        /// <summary>输入连接器：通讯实例 + inflag(可选) + 写入变量列表</summary>
        public ObservableCollection<ConnectorViewModel> Inputs { get; } = new()
        {
            new ConnectorViewModel
            {
                Title = "Instance",
                DataType = DataType.TCPAgreement,
                Direction = ConnectorDirection.Input
            }
        };

        /// <summary>输出连接器：outflag(可选)</summary>
        public ObservableCollection<ConnectorViewModel> Outputs { get; } = new();

        #endregion

        #region inflag / outflag 属性

        public bool IsInFlagEnabled
        {
            get => _isInFlagEnabled;
            set
            {
                if (_isInFlagEnabled == value) return;
               SetProperty(ref _isInFlagEnabled, value);
                SyncInFlagConnector();
                RaisePropertyChanged(nameof(InputInfo));
            }
        }

        public DataType InFlagDataType
        {
            get => _inFlagDataType;
            set
            {
                if (_inFlagDataType == value) return;
               SetProperty(ref  _inFlagDataType, value);
                RaisePropertyChanged(nameof(InputInfo));
                if (IsInFlagEnabled)
                {
                    var inflag = GetInFlagConnector();
                    if (inflag != null)
                    {
                        DisconnectConnector(inflag);
                        inflag.DataType = value;
                    }
                }
            }
        }

        public bool IsOutFlagEnabled
        {
            get => _isOutFlagEnabled;
            set
            {
                if (_isOutFlagEnabled == value) return;
                SetProperty(ref _isOutFlagEnabled ,value);
                SyncOutFlagConnector();
                RaisePropertyChanged(nameof(OutputInfo));
                RaisePropertyChanged(nameof(OutFlagValueText));
            }
        }

        public DataType OutFlagDataType
        {
            get => _outFlagDataType;
            set
            {
                if (_outFlagDataType == value) return;
                SetProperty(ref _outFlagDataType, value);
                RaisePropertyChanged(nameof(OutputInfo));
                RaisePropertyChanged(nameof(OutFlagValueText));
                if (IsOutFlagEnabled)
                {
                    var outflag = GetOutFlagConnector();
                    if (outflag != null)
                    {
                        DisconnectConnector(outflag);
                        outflag.DataType = value;
                        outflag.Value = GetDefaultOutFlagValue();
                    }
                }
            }
        }

        #endregion

        #region Flag 连接器管理

        public static DataType[] AllDataTypes { get; } = Enum
            .GetValues(typeof(DataType))
            .Cast<DataType>()
            .ToArray();

        /// <summary>供 XAML 绑定：inflag\outflag DataType 选项列表</summary>
        public static DataType[] InOutDataTypes { get; } = [DataType.Bool, DataType.Int16, DataType.String];

        /// <summary>获取 inflag 连接器（索引 1，索引 0 为通讯实例）</summary>
        // 修复后（按 Title）
        private ConnectorViewModel? GetInFlagConnector()
        {
            return Inputs.FirstOrDefault(c => c.Title == "InFlag");
        }

        /// <summary>获取 outflag 连接器</summary>
        private ConnectorViewModel? GetOutFlagConnector()
        {
            return Outputs.Count > 0 ? Outputs[0] : null;
        }

        /// <summary>获取变量对应的输入连接器起始索引（通讯实例 + inflag 之后）</summary>
        private int VariableInputStartIndex => 1;

        /// <summary>获取指定变量的输入连接器</summary>
        private ConnectorViewModel? GetVariableInputConnector(WriteVariable variable)
        {
            var idx = _writeVariables.IndexOf(variable);
            var startIdx = VariableInputStartIndex;
            var targetIdx = startIdx + idx;
            return targetIdx >= startIdx && targetIdx < Inputs.Count ? Inputs[targetIdx] : null;
        }

        private void SyncInFlagConnector()
        {
            if (IsInFlagEnabled)
            {
                if (GetInFlagConnector() == null)
                {
                    Inputs.Add(new ConnectorViewModel
                    {
                        Title = "InFlag",
                        DataType = InFlagDataType,
                        Direction = ConnectorDirection.Input
                    });
                }
            }
            else
            {
                var inflag = GetInFlagConnector();
                if (inflag != null)
                {
                    DisconnectConnector(inflag);
                    Inputs.Remove(inflag);
                }
            }
        }

        private void SyncOutFlagConnector()
        {
            if (IsOutFlagEnabled)
            {
                if (GetOutFlagConnector() == null)
                {
                    Outputs.Add(new ConnectorViewModel
                    {
                        Title = "OutFlag",
                        DataType = OutFlagDataType,
                        Direction = ConnectorDirection.Output,
                        Value = GetDefaultOutFlagValue()
                    });
                }
            }
            else
            {
                if (Outputs.Count > 0)
                {
                    var outflag = Outputs[0];
                    DisconnectConnector(outflag);
                    Outputs.RemoveAt(0);
                }
            }
        }

        private object? GetDefaultOutFlagValue()
        {
            return OutFlagDataType switch
            {
                DataType.Bool => false,
                DataType.Int16 => 0,
                DataType.Double => 0.0,
                DataType.String => "",
                _ => null
            };
        }

        public string? OutFlagValueText
        {
            get
            {
                if (!IsOutFlagEnabled) return null;
                var outflag = GetOutFlagConnector();
                return outflag?.Value?.ToString() ?? "null";
            }
        }

        #endregion

        #region 变量管理

        public ObservableCollection<WriteVariable> WriteVariables => _writeVariables;

        public void AddVariable()
        {
            var variable = new WriteVariable
            {
                Name = $"变量{_writeVariables.Count + 1}",
                DataType = DataType.String,
                Address = $"{_writeVariables.Count}"
            };
            variable.PropertyChanged += OnVariablePropertyChanged;
            _writeVariables.Add(variable);

            var connector = new ConnectorViewModel
            {
                Title = variable.Name,
                DataType = variable.DataType,
                Direction = ConnectorDirection.Input
            };
            var insertIdx = Inputs.Count - (IsInFlagEnabled ? 1 : 0);
            Inputs.Insert(insertIdx, connector);

            RaisePropertyChanged(nameof(InputInfo));
        }

        public void RemoveVariable(WriteVariable variable)
        {
            variable.PropertyChanged -= OnVariablePropertyChanged;

            var connector = GetVariableInputConnector(variable);
            if (connector != null)
            {
                DisconnectConnector(connector);
                Inputs.Remove(connector);
            }

            _writeVariables.Remove(variable);


            RaisePropertyChanged(nameof(InputInfo));
        }

        private void OnVariablePropertyChanged(object? sender, PropertyChangedEventArgs e)
        {
            if (sender is not WriteVariable variable) return;
            var connector = GetVariableInputConnector(variable);
            if (connector == null) return;

            switch (e.PropertyName)
            {
                case nameof(WriteVariable.Name):
                    connector.Title = variable.Name;
                    break;
                case nameof(WriteVariable.DataType):
                    DisconnectConnector(connector);//数据类型切换时，断开已有连线
                    connector.DataType = variable.DataType;
                    break;
            }
        }

        #endregion

        #region 导入导出

        private void ImportVariables()
        {
            var dialog = new Microsoft.Win32.OpenFileDialog
            {
                Title = "导入变量",
                Filter = "CSV/Excel 文件 (*.csv;*.xlsx)|*.csv;*.xlsx|CSV 文件 (*.csv)|*.csv|Excel 文件 (*.xlsx)|*.xlsx|所有文件 (*.*)|*.*",
                DefaultExt = ".csv"
            };

            if (dialog.ShowDialog() != true) return;

            try
            {
                var ext = Path.GetExtension(dialog.FileName).ToLowerInvariant();
                if (ext == ".xlsx")
                    ImportFromExcel(dialog.FileName);
                else
                    ImportFromCsv(dialog.FileName);

                RaisePropertyChanged(nameof(InputInfo));
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[TcpsWriteOperationViewModel] 导入失败: {ex.Message}");
            }
        }

        private void ImportFromCsv(string filePath)
        {
            var lines = File.ReadAllLines(filePath);
            if (lines.Length < 1) return;

            foreach (var line in lines)
            {
                if (string.IsNullOrWhiteSpace(line)) continue;

                var parts = line.Split(',');
                if (parts.Length < 3) continue;

                var name = parts[0].Trim();
                if (string.IsNullOrEmpty(name)) continue;

                if (!Enum.TryParse<DataType>(parts[1].Trim(), true, out var dataType))
                    dataType = DataType.String;

                var address = parts[2].Trim();

                AddVariableInternal(name, dataType, address);
            }
        }

        private void ImportFromExcel(string filePath)
        {

            using var package = new ExcelPackage(new FileInfo(filePath));
            var sheet = package.Workbook.Worksheets.FirstOrDefault();
            if (sheet == null) return;

            for (int row = 2; row <= sheet.Dimension.End.Row; row++)
            {
                var name = sheet.Cells[row, 1].Text.Trim();
                if (string.IsNullOrEmpty(name)) continue;

                var typeStr = sheet.Cells[row, 2].Text.Trim();
                if (!Enum.TryParse<DataType>(typeStr, true, out var dataType))
                    dataType = DataType.String;

                var address = sheet.Cells[row, 3].Text.Trim();

                AddVariableInternal(name, dataType, address);
            }
        }

        private void AddVariableInternal(string name, DataType dataType, string address)
        {
            var variable = new WriteVariable
            {
                Name = name,
                DataType = dataType,
                Address = address,
            };
            variable.PropertyChanged += OnVariablePropertyChanged;
            _writeVariables.Add(variable);

            var connector = new ConnectorViewModel
            {
                Title = variable.Name,
                DataType = variable.DataType,
                Direction = ConnectorDirection.Input
            };
            // 插入在变量列表末尾、inflag 之前
            var insertIdx = Inputs.Count - (IsInFlagEnabled ? 1 : 0);
            Inputs.Insert(insertIdx, connector);
        }

        private void ExportVariables()
        {
            if (_writeVariables.Count == 0) return;

            var dialog = new Microsoft.Win32.SaveFileDialog
            {
                Title = "导出变量",
                Filter = "CSV 文件 (*.csv)|*.csv|Excel 文件 (*.xlsx)|*.xlsx",
                DefaultExt = ".csv",
                FileName = "variables.csv"
            };

            if (dialog.ShowDialog() != true) return;

            try
            {
                var ext = Path.GetExtension(dialog.FileName).ToLowerInvariant();
                if (ext == ".xlsx")
                    ExportToExcel(dialog.FileName);
                else
                    ExportToCsv(dialog.FileName);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[TcpsWriteOperationViewModel] 导出失败: {ex.Message}");
            }
        }

        private void ExportToCsv(string filePath)
        {
            var lines = _writeVariables.Select(v =>
                $"{v.Name},{v.DataType},{v.Address}");
            File.WriteAllLines(filePath, lines);
        }

        private void ExportToExcel(string filePath)
        {
            using var package = new ExcelPackage();
            var sheet = package.Workbook.Worksheets.Add("变量列表");

            sheet.Cells[1, 1].Value = "名称";
            sheet.Cells[1, 2].Value = "类型";
            sheet.Cells[1, 3].Value = "地址";

            using (var headerRange = sheet.Cells[1, 1, 1, 3])
            {
                headerRange.Style.Font.Bold = true;
                headerRange.Style.Fill.PatternType = OfficeOpenXml.Style.ExcelFillStyle.Solid;
                headerRange.Style.Fill.BackgroundColor.SetColor(System.Drawing.Color.FromArgb(49, 50, 68));
                headerRange.Style.Font.Color.SetColor(System.Drawing.Color.FromArgb(137, 180, 250));
            }

            for (int i = 0; i < _writeVariables.Count; i++)
            {
                var v = _writeVariables[i];
                var row = i + 2;
                sheet.Cells[row, 1].Value = v.Name;
                sheet.Cells[row, 2].Value = v.DataType.ToString();
                sheet.Cells[row, 3].Value = v.Address;
            }

            sheet.Column(1).AutoFit();
            sheet.Column(2).AutoFit();
            sheet.Column(3).AutoFit();
            sheet.Column(4).AutoFit();

            package.SaveAs(new FileInfo(filePath));
        }

        #endregion

        #region 通讯属性

        public ICommunication? CommunicationInstance
        {
            get => _communicationInstance;
            private set
            {
                SetProperty(ref _communicationInstance, value);
                RaisePropertyChanged(nameof(InputInfo));
                RaisePropertyChanged(nameof(OutputInfo));
            }
        }

        public bool IsConnected
        {
            get => _isConnected;
            set
            {
                SetProperty(ref _isConnected, value);
                RaisePropertyChanged(nameof(InputInfo));
                RaisePropertyChanged(nameof(OutputInfo));
            }
        }

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

        public string? InputInfo
        {
            get
            {
                if (CommunicationInstance == null && !IsInFlagEnabled && _writeVariables.Count == 0)
                    return null;

                var sb = new StringBuilder();
                if (CommunicationInstance != null)
                {
                    sb.AppendLine("{");
                    sb.AppendLine($"  \"类型\": \"{CommunicationInstance.GetType().Name}\",");
                    sb.AppendLine($"  \"连接字符串\": \"{CommunicationInstance.ConnectionString}\",");
                    sb.AppendLine($"  \"状态\": \"{(IsConnected ? "已连接" : "未连接")}\",");
                    sb.AppendLine($"  \"Key\": \"{CommunicationInstance.Key}\"");
                    sb.Append("}");
                }

                for (int i = 0; i < _writeVariables.Count; i++)
                {
                    var v = _writeVariables[i];
                    var val = v.Value?.ToString() ?? "null";
                    //sb.AppendLine($"[{i}] {v.Name} {v.DataType} {val}");
                }

                if (IsInFlagEnabled)
                {
                    if (sb.Length > 0) sb.AppendLine();
                    var inflag = GetInFlagConnector();
                    var val = inflag?.Value?.ToString() ?? "null";
                    //sb.Append($"{InFlagDataType} : InFlag = {val}");
                }

                return sb.Length > 0 ? sb.ToString() : null;
            }
        }

        public string? OutputInfo
        {
            get
            {
                if (_writeVariables.Count == 0 && !IsOutFlagEnabled)
                    return null;

                var sb = new StringBuilder();
                for (int i = 0; i < _writeVariables.Count; i++)
                {
                    var v = _writeVariables[i];
                    var val = v.Value?.ToString() ?? "null";
                    sb.AppendLine($"[{i}] {v.Name} {v.DataType} {val}");
                }

                if (IsOutFlagEnabled)
                {
                    var outflag = GetOutFlagConnector();
                    var val = outflag?.Value?.ToString() ?? "null";
                    sb.AppendLine($"outflag {OutFlagDataType} {val}");
                }

                return sb.ToString().TrimEnd();
            }
        }

        public string InstanceInfo
        {
            get
            {
                if (CommunicationInstance == null)
                    return "未连接通讯实例";

                var info = $"连接: {CommunicationInstance.ConnectionString}\n";
                info += $"变量: {_writeVariables.Count} 个";
                if (IsInFlagEnabled || IsOutFlagEnabled)
                {
                    info += "\n";
                    if (IsInFlagEnabled) info += $"in: {InFlagDataType}";
                    if (IsInFlagEnabled && IsOutFlagEnabled) info += " | ";
                    if (IsOutFlagEnabled) info += $"out: {OutFlagDataType}";
                }
                if (!string.IsNullOrEmpty(StatusMessage))
                    info += $"\n{StatusMessage}";
                return info;
            }
        }

        #endregion

        #region IExecutableOperation 实现

        public bool Execute()
        {
            RunStatusMessage(NodeRunStatus.Disabled, string.Empty);
            try
            {
                //清空输出数据
                foreach (var op in Outputs)
                {
                    op.Value = null;
                }
                // 从输入连接器获取通讯实例
                if (Inputs.Count == 0 || Inputs[0].Value is not ICommunication comm)
                {
                    RunStatusMessage(NodeRunStatus.Error, "无通讯实例输入");
                    SetOutFlagValue(false);
                    return false;
                }

                CommunicationInstance = comm;

                // 检查连接状态
                comm.OnStateChanged += OnCommunicationStateChanged;
                IsConnected = comm.IsConnected;
                comm.OnStateChanged -= OnCommunicationStateChanged;

                if (!IsConnected)
                {
                    RunStatusMessage(NodeRunStatus.Error, "通讯未连接");
                    SetOutFlagValue(false);
                    return false;
                }

                // inflag 逻辑
                if (IsInFlagEnabled && GetInFlagConnector()?.IsConnected == true)
                {
                    var inflagValue = GetInFlagConnector()!.Value;
                    if (inflagValue is bool inflagBool && !inflagBool)
                    {
                        RunStatusMessage(NodeRunStatus.Error, "InFlag 阻止执行");

                        SetOutFlagValue(false);
                        return false;
                    }
                    if (inflagValue == null)
                    {
                        RunStatusMessage(NodeRunStatus.Error, "InFlag 无有效值");
                        SetOutFlagValue(false);
                        return false;
                    }
                }

                if (_writeVariables.Count == 0)
                {
                    RunStatusMessage(NodeRunStatus.Error, "未配置写入变量");
                    SetOutFlagValue(false);
                    return false;
                }
                // 从输入连接器读取变量值并写入设备
                var successCount = 0;
                for (int i = 0; i < _writeVariables.Count; i++)
                {
                    var variable = _writeVariables[i];
                    var connector = GetVariableInputConnector(variable);

                    try
                    {
                        // 从输入连接器获取要写入的值
                        var valueToWrite = connector?.Value;

                        var result = WriteData(comm, variable, valueToWrite);
                        if (result)
                        {
                            variable.Value = valueToWrite;
                            successCount++;
                        }
                    }
                    catch (Exception ex)
                    {
                        System.Diagnostics.Debug.WriteLine(
                            $"[TcpsWriteOperationViewModel] 写入变量 '{variable.Name}' 失败: {ex.Message}");
                    }
                }

                var allSuccess = successCount == _writeVariables.Count;
                //StatusMessage = $"写入 {successCount}/{_writeVariables.Count}";
                RaisePropertyChanged(nameof(InputInfo));
                RaisePropertyChanged(nameof(OutputInfo));
                RaisePropertyChanged(nameof(InstanceInfo));
                SetOutFlagValue(allSuccess);
                //RunStatusMessage(NodeRunStatus.Completed, "执行成功");
                return allSuccess;
            }
            catch (Exception ex)
            {
                RunStatusMessage(NodeRunStatus.Error, $"异常: {ex.Message}");
                System.Diagnostics.Debug.WriteLine($"[TcpsWriteOperationViewModel] Error: {ex.Message}");
                SetOutFlagValue(false);
                return false;
            }
        }

        private void SetOutFlagValue(bool success)
        {
            if (!IsOutFlagEnabled) return;
            var outflag = GetOutFlagConnector();
            if (outflag == null) return;

            outflag.Value = OutFlagDataType switch
            {
                DataType.Bool => success,
                DataType.Int16 => success ? 1 : 0,
                DataType.String => success ? "OK" : "FAIL",
                _ => success
            };
            RaisePropertyChanged(nameof(OutFlagValueText));
        }

        private void OnCommunicationStateChanged(object? sender, CommunicationState state)
        {
            IsConnected = state == CommunicationState.Connected;
        }

        /// <summary>
        /// 向通讯实例写入数据
        /// 优先使用协议特定的类型化写入方法，协议不支持时回退到 SendAsync
        /// </summary>
        protected virtual bool WriteData(ICommunication comm, WriteVariable variable, object? value)
        {
            try
            {
                if (value == null) return false;

                var task = Task.Run(async () =>
                {
                    try
                    {
                        return variable.DataType switch
                        {
                            DataType.Bool => await comm.WriteAsync<bool>(variable.Address, (bool)value),
                            DataType.Int16 => await comm.WriteAsync<short>(variable.Address, Convert.ToInt16(value)),
                            DataType.UInt16 => await comm.WriteAsync<ushort>(variable.Address, Convert.ToUInt16(value)),
                            DataType.Int32 => await comm.WriteAsync<int>(variable.Address, Convert.ToInt32(value)),
                            DataType.UInt32 => await comm.WriteAsync<uint>(variable.Address, Convert.ToUInt32(value)),
                            DataType.Int64 => await comm.WriteAsync<long>(variable.Address, Convert.ToInt64(value)),
                            DataType.UInt64 => await comm.WriteAsync<ulong>(variable.Address, Convert.ToUInt64(value)),
                            DataType.Float => await comm.WriteAsync<float>(variable.Address, Convert.ToSingle(value)),
                            DataType.Double => await comm.WriteAsync<double>(variable.Address, Convert.ToDouble(value)),
                            DataType.String => await comm.WriteStringAsync(variable.Address, value.ToString() ?? ""),
                            _ => false
                        };
                    }
                    catch (NotSupportedException)
                    {
                        // 协议不支持类型化写入，回退到 SendAsync
                        System.Diagnostics.Debug.WriteLine(
                            $"[WriteData] {comm.GetType().Name} 不支持类型化写入，回退到 SendAsync");
                        var data = value.ToString() ?? "";
                        return await comm.SendAsync(data);
                    }
                });

                return task.Wait(TimeSpan.FromMilliseconds(5000)) && task.Result;
            }
            catch
            {
                return false;
            }
        }

        #endregion

        #region ISerializableOperation 实现

        private const int SerializeVersion = 1;

        public void Serialize(BinaryWriter writer)
        {
            writer.Write(SerializeVersion);
            writer.Write(Title);
            writer.Write(_writeVariables.Count);
            foreach (var v in _writeVariables)
            {
                writer.Write(v.Name);
                writer.Write((int)v.DataType);
                writer.Write(v.Address);
            }
            writer.Write(IsInFlagEnabled);
            writer.Write((int)InFlagDataType);
            writer.Write(IsOutFlagEnabled);
            writer.Write((int)OutFlagDataType);
        }

        public void Deserialize(BinaryReader reader)
        {
            var version = reader.ReadInt32();
            Title = reader.ReadString();

            var count = reader.ReadInt32();
            for (int i = 0; i < count; i++)
            {
                var name = reader.ReadString();
                var dataType = (DataType)reader.ReadInt32();
                var address = reader.ReadString();

                AddVariableInternal(name, dataType, address);
            }

            _isInFlagEnabled = reader.ReadBoolean();
            _inFlagDataType = (DataType)reader.ReadInt32();
            _isOutFlagEnabled = reader.ReadBoolean();
            _outFlagDataType = (DataType)reader.ReadInt32();

            if (_isInFlagEnabled) SyncInFlagConnector();
            if (_isOutFlagEnabled) SyncOutFlagConnector();

            RaisePropertyChanged(nameof(IsInFlagEnabled));
            RaisePropertyChanged(nameof(InFlagDataType));
            RaisePropertyChanged(nameof(IsOutFlagEnabled));
            RaisePropertyChanged(nameof(OutFlagDataType));
        }

        #endregion

        #region INotifyPropertyChanged

        //public event PropertyChangedEventHandler? PropertyChanged;

        //protected void OnPropertyChanged(string propertyName)
        //{
        //    PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        //}

        #endregion

        #region IConnectionAware 实现

        public ObservableCollection<ConnectionViewModel> Connections
        {
            get => _connections ??= new ObservableCollection<ConnectionViewModel>();
            set => _connections = value;
        }

        public void DisconnectConnector(ConnectorViewModel connector)
        {
            if (_connections == null || connector == null) return;

            var toRemove = _connections.Where(c =>
                c.Input == connector || c.Output == connector).ToList();

            if (toRemove.Count == 0) return;

            foreach (var conn in toRemove)
            {
                if (conn.Output != null && !_connections.Any(c => c != conn && c.Output == conn.Output))
                    conn.Output.IsConnected = false;
                if (conn.Input != null && !_connections.Any(c => c != conn && c.Input == conn.Input))
                    conn.Input.IsConnected = false;
                _connections.Remove(conn);
            }

            System.Diagnostics.Debug.WriteLine(
                $"[TcpsWriteOperationViewModel] 已断开 '{connector.Title}' 上的 {toRemove.Count} 条连线");
        }

        #endregion
    }

    /// <summary>
    /// 简易 RelayCommand 实现
    /// </summary>
    internal class RelayCommand<T> : ICommand
    {
        private readonly Action<T> _execute;
        private readonly Func<T, bool>? _canExecute;

        public RelayCommand(Action<T> execute, Func<T, bool>? canExecute = null)
        {
            _execute = execute ?? throw new ArgumentNullException(nameof(execute));
            _canExecute = canExecute;
        }

        public bool CanExecute(object? parameter)
        {
            if (_canExecute == null) return true;
            if (parameter == null) return _canExecute(default!);
            if (parameter is T t) return _canExecute(t);
            return false;
        }

        public void Execute(object? parameter)
        {
            if (parameter == null)
                _execute(default!);
            else if (parameter is T t)
                _execute(t);
        }

        public event EventHandler? CanExecuteChanged
        {
            add => CommandManager.RequerySuggested += value;
            remove => CommandManager.RequerySuggested -= value;
        }
    }
}