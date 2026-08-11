using System;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
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

namespace TcpsReadPlugin
{
    /// <summary>
    /// TCP 读取插件操作（节点实例）
    /// 输入：TCPAgreement（ICommunication 通讯实例）
    /// 输出：动态注册的变量列表，每个变量对应一个输出连接器
    /// 实现 IExecutableOperation 接口，点击执行按钮时自动调用 Execute()
    /// </summary>
    public class TcpsReadOperationViewModel : NotifyPropertyBase, IExecutableOperation, ISerializableOperation, IConnectionAware
    {
        #region 字段

        private string _title = "TCP 读取";
        private string? _statusMessage;
        private NodeRunStatus _runStatus;
        private ICommunication? _communicationInstance;
        private bool _isConnected;

        private string _icon = "M183.0912 179.456 L378.7776 369.664 L366.336 381.7984 L170.6496 191.5904 L183.0912 179.456 Z M665.3952 151.7568 L577.28 406.7328 L560.5888 401.3056 L648.6528 146.3296 L665.3952 151.7568 Z M874.6496 560.5888 L597.9648 561.7152 L597.8624 544.6144 L874.5984 543.488 L874.6496 560.5888 Z M665.3952 835.9424 L544.8192 593.7664 L560.6912 586.3424 L681.216 828.4672 L665.3952 835.9424 Z M352.5664 627.2 C340.0192 586.4448 320.0064 546.3296 320.0064 522.4448 C320.0064 418.56 405.9712 334.3872 512.0064 334.3872 C618.0416 334.3872 704.0064 418.56 704.0064 522.4448 C704.0064 626.3296 618.0416 710.5536 512.0064 710.5536 C454.9856 710.5536 400.2816 679.936 364.0192 639.024 L215.6928 759.344 C228.3392 777.6224 235.6608 799.6896 235.6608 823.4464 C235.6608 886.9344 183.1296 940.3904 118.3616 940.3904 C53.5904 940.3904 0 888.9344 0 825.4464 C0 761.9584 53.5904 709.5536 118.3616 709.5536 C152.6144 709.5536 183.3824 723.8896 204.8896 746.8288 L353.5264 626.2016 Z M0 114.944 C0 51.456 52.5312 0 117.3504 0 C182.1184 0 234.6496 51.456 234.6496 114.944 C234.6496 178.432 182.1184 229.888 117.3504 229.888 C52.5312 229.888 0 178.432 0 114.944 Z M554.6496 114.944 C554.6496 51.456 607.232 0 672 0 C736.8192 0 789.3504 51.456 789.3504 114.944 C789.3504 178.432 736.8192 229.888 672 229.888 C607.232 229.888 554.6496 178.432 554.6496 114.944 Z M789.3504 574.6688 C789.3504 511.232 841.8816 459.776 906.6496 459.776 C971.4688 459.776 1024 511.232 1024 574.6688 C1024 638.1568 971.4688 690.6144 906.6496 690.6144 C841.8816 690.6144 789.3504 638.1568 789.3504 574.6688 Z M597.3504 909.0048 C597.3504 845.5168 649.8816 794.0608 714.6496 794.0608 C779.4688 794.0608 832 845.5168 832 909.0048 C832 972.4928 779.4688 1024 714.6496 1024 C649.8816 1024 597.3504 972.4928 597.3504 909.0048 Z";


        // inflag / outflag 开关
        private bool _isInFlagEnabled;
        private bool _isOutFlagEnabled;
        private DataType _inFlagDataType = DataType.Bool;
        private DataType _outFlagDataType = DataType.Bool;

        // IConnectionAware 实现
        private ObservableCollection<ConnectionViewModel>? _connections;

        // 变量列表（用户可动态增删）
        private readonly ObservableCollection<ReadVariable> _readVariables = new();

        #endregion

        #region 命令

        /// <summary>添加变量命令</summary>
        public ICommand AddVariableCommand { get; }

        /// <summary>移除变量命令（参数为 ReadVariable）</summary>
        public ICommand RemoveVariableCommand { get; }

        /// <summary>从 CSV/Excel 导入变量命令</summary>
        public ICommand ImportVariablesCommand { get; }

        /// <summary>导出变量到 CSV 命令</summary>
        public ICommand ExportVariablesCommand { get; }

        /// <summary>构造函数</summary>
        public TcpsReadOperationViewModel()
        {
            AddVariableCommand = new RelayCommand<object?>(_ => AddVariable());
            RemoveVariableCommand = new RelayCommand<ReadVariable>(RemoveVariable);
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

        /// <summary>输入连接器（接收 TCPAgreement 类型的通讯实例，可选 inflag）</summary>
        public ObservableCollection<ConnectorViewModel> Inputs { get; } = new()
        {
            new ConnectorViewModel
            {
                Title = "Instance",
                DataType = DataType.TCPAgreement,
                Direction = ConnectorDirection.Input
            }
        };

        /// <summary>输出连接器（动态生成变量输出 + 可选 outflag）</summary>
        public ObservableCollection<ConnectorViewModel> Outputs { get; } = new();

        #endregion

        #region inflag / outflag 属性

        /// <summary>是否启用输入 Flag（在参数面板勾选启用）</summary>
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

        /// <summary>输入 Flag 的数据类型（修改时断开 inflag 上所有连线）</summary>
        public DataType InFlagDataType
        {
            get => _inFlagDataType;
            set
            {
                if (_inFlagDataType == value) return;
                SetProperty(ref _inFlagDataType, value);
                RaisePropertyChanged(nameof(InputInfo));
                if (IsInFlagEnabled)
                {
                    // 更新已存在的 inflag 连接器类型
                    var inflag = GetInFlagConnector();
                    if (inflag != null)
                    {
                        // 先断开该连接器上所有连线，再修改类型
                        DisconnectConnector(inflag);
                        inflag.DataType = value;
                    }
                }
            }
        }

        /// <summary>是否启用输出 Flag（在参数面板勾选启用）</summary>
        public bool IsOutFlagEnabled
        {
            get => _isOutFlagEnabled;
            set
            {
                if (_isOutFlagEnabled == value) return;
                SetProperty(ref _isOutFlagEnabled , value);
                SyncOutFlagConnector();
                RaisePropertyChanged(nameof(OutputInfo));
                RaisePropertyChanged(nameof(OutFlagValueText));
            }
        }

        /// <summary>输出 Flag 的数据类型（修改时断开 outflag 上所有连线）</summary>
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
                        // 先断开该连接器上所有连线，再修改类型
                        DisconnectConnector(outflag);
                        outflag.DataType = value;
                        outflag.Value = GetDefaultOutFlagValue();
                    }
                }
            }
        }

        #endregion

        #region Flag 连接器管理

        /// <summary>供 XAML 绑定：DataType 选项列表</summary>
        public static DataType[] AllDataTypes { get; } = Enum
            .GetValues(typeof(DataType))
            .Cast<DataType>()
            .ToArray();
        /// <summary>供 XAML 绑定：inflag\outflag DataType 选项列表</summary>
        public static DataType[] InOutDataTypes { get; } = [DataType.Bool, DataType.Int16, DataType.String];


        /// <summary>获取 inflag 连接器（索引 1，索引 0 为通讯实例）</summary>
        private ConnectorViewModel? GetInFlagConnector()
        {
            return Inputs.Count > 1 ? Inputs[1] : null;
        }

        /// <summary>获取 outflag 连接器（最后一个输出）</summary>
        private ConnectorViewModel? GetOutFlagConnector()
        {
            // outflag 是最后一个输出，排在所有变量输出之后
            return Outputs.Count > _readVariables.Count
                ? Outputs[^1]
                : null;
        }

        /// <summary>同步 inflag 连接器：启用时添加，禁用时移除</summary>
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
                if (Inputs.Count > 1)
                {
                    // 先断开 inflag 上的所有连线，再移除连接器
                    var inflag = Inputs[1];
                    DisconnectConnector(inflag);
                    Inputs.RemoveAt(1);
                }
            }
        }

        /// <summary>同步 outflag 连接器：启用时添加（默认在输出节点最下方），禁用时移除</summary>
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
                if (Outputs.Count > _readVariables.Count)
                {
                    // 先断开 outflag 上的所有连线，再移除连接器
                    var outflag = Outputs[^1];
                    DisconnectConnector(outflag);
                    Outputs.RemoveAt(Outputs.Count - 1);
                }
            }
        }

        /// <summary>获取 outflag 默认初始值</summary>
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

        /// <summary>outflag 当前值文本（供 XAML 绑定，仅返回值的字符串表示）</summary>
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

        /// <summary>读取变量集合（参数面板绑定）</summary>
        public ObservableCollection<ReadVariable> ReadVariables => _readVariables;

        /// <summary>添加一个读取变量，并同步创建对应的输出连接器（插入在 outflag 之前）</summary>
        public void AddVariable()
        {
            var variable = new ReadVariable
            {
                Name = $"变量{_readVariables.Count + 1}",
                DataType = DataType.String,
                Address = $"{_readVariables.Count}"
            };
            variable.PropertyChanged += OnVariablePropertyChanged;
            _readVariables.Add(variable);

            var connector = new ConnectorViewModel
            {
                Title = variable.Name,
                DataType = variable.DataType,
                Direction = ConnectorDirection.Output
            };
            // 插入在变量列表末尾、outflag 之前（如果 outflag 存在）
            var insertIdx = Outputs.Count - (IsOutFlagEnabled ? 1 : 0);
            Outputs.Insert(insertIdx, connector);

            RaisePropertyChanged(nameof(OutputInfo));
        }

        /// <summary>移除指定变量，并同步移除对应的输出连接器及其连线</summary>
        public void RemoveVariable(ReadVariable variable)
        {
            variable.PropertyChanged -= OnVariablePropertyChanged;

            // 找到对应的输出连接器，先断开连线再移除
            var idx = _readVariables.IndexOf(variable);
            _readVariables.Remove(variable);

            if (idx >= 0 && idx < Outputs.Count)
            {
                DisconnectConnector(Outputs[idx]);
                Outputs.RemoveAt(idx);
            }

            RaisePropertyChanged(nameof(OutputInfo));
        }

        /// <summary>变量属性变化时同步更新对应连接器</summary>
        private void OnVariablePropertyChanged(object? sender, PropertyChangedEventArgs e)
        {
            if (sender is not ReadVariable variable) return;
            var idx = _readVariables.IndexOf(variable);
            if (idx < 0 || idx >= Outputs.Count) return;

            var connector = Outputs[idx];
            switch (e.PropertyName)
            {
                case nameof(ReadVariable.Name):
                    connector.Title = variable.Name;
                    break;
                case nameof(ReadVariable.DataType):
                    DisconnectConnector(connector);
                    connector.DataType = variable.DataType;
                    break;
            }
        }

        #endregion

        #region 导入导出

        /// <summary>
        /// 从 CSV 或 Excel 文件导入变量（自动识别文件类型）
        /// CSV 格式：变量名,数据类型,地址
        /// Excel 格式：第一行为表头，后续行为数据（A列=名称, B列=类型, C列=地址）
        /// </summary>
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

                RaisePropertyChanged(nameof(OutputInfo));
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[TcpsReadOperationViewModel] 导入失败: {ex.Message}");
            }
        }

        /// <summary>从 CSV 文件解析变量</summary>
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

                ushort strLen = 256;
                if (parts.Length >= 4 && ushort.TryParse(parts[3].Trim(), out var len))
                    strLen = len;

                AddVariableInternal(name, dataType, parts[2].Trim(), strLen);
            }
        }

        /// <summary>从 Excel (.xlsx) 文件解析变量（使用 EPPlus）</summary>
        private void ImportFromExcel(string filePath)
        {

            using var package = new ExcelPackage(new FileInfo(filePath));
            var sheet = package.Workbook.Worksheets.FirstOrDefault();
            if (sheet == null) return;

            // 从第 2 行开始读取（跳过表头）
            for (int row = 2; row <= sheet.Dimension.End.Row; row++)
            {
                var name = sheet.Cells[row, 1].Text.Trim();
                if (string.IsNullOrEmpty(name)) continue;

                var typeStr = sheet.Cells[row, 2].Text.Trim();
                if (!Enum.TryParse<DataType>(typeStr, true, out var dataType))
                    dataType = DataType.String;

                var address = sheet.Cells[row, 3].Text.Trim();

                ushort strLen = 256;
                if (ushort.TryParse(sheet.Cells[row, 4].Text.Trim(), out var len))
                    strLen = len;

                AddVariableInternal(name, dataType, address, strLen);
            }
        }

        /// <summary>内部方法：创建变量和对应的输出连接器（插入在 outflag 之前）</summary>
        private void AddVariableInternal(string name, DataType dataType, string address, ushort stringLength = 256)
        {
            var variable = new ReadVariable
            {
                Name = name,
                DataType = dataType,
                Address = address,
                StringLength = stringLength
            };
            variable.PropertyChanged += OnVariablePropertyChanged;
            _readVariables.Add(variable);

            var connector = new ConnectorViewModel
            {
                Title = variable.Name,
                DataType = variable.DataType,
                Direction = ConnectorDirection.Output
            };
            // 插入在变量列表末尾、outflag 之前
            var insertIdx = Outputs.Count - (IsOutFlagEnabled ? 1 : 0);
            Outputs.Insert(insertIdx, connector);
        }

        /// <summary>
        /// 导出变量到 CSV 或 Excel 文件（默认 CSV）
        /// </summary>
        private void ExportVariables()
        {
            if (_readVariables.Count == 0) return;

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
                System.Diagnostics.Debug.WriteLine($"[TcpsReadOperationViewModel] 导出失败: {ex.Message}");
            }
        }

        /// <summary>导出到 CSV 文件</summary>
        private void ExportToCsv(string filePath)
        {
            var lines = _readVariables.Select(v =>
                 $"{v.Name},{v.DataType},{v.Address},{v.StringLength}");
            File.WriteAllLines(filePath, lines);
        }

        /// <summary>导出到 Excel (.xlsx) 文件（使用 EPPlus）</summary>
        private void ExportToExcel(string filePath)
        {

            using var package = new ExcelPackage();
            var sheet = package.Workbook.Worksheets.Add("变量列表");

            // 表头
            sheet.Cells[1, 1].Value = "名称";
            sheet.Cells[1, 2].Value = "类型";
            sheet.Cells[1, 3].Value = "地址";
            sheet.Cells[1, 4].Value = "长度";

            // 表头样式
            using (var headerRange = sheet.Cells[1, 1, 1, 4])
            {
                headerRange.Style.Font.Bold = true;
                headerRange.Style.Fill.PatternType = OfficeOpenXml.Style.ExcelFillStyle.Solid;
                headerRange.Style.Fill.BackgroundColor.SetColor(System.Drawing.Color.FromArgb(49, 50, 68));
                headerRange.Style.Font.Color.SetColor(System.Drawing.Color.FromArgb(137, 180, 250));
            }

            // 数据行
            for (int i = 0; i < _readVariables.Count; i++)
            {
                var v = _readVariables[i];
                var row = i + 2;
                sheet.Cells[row, 1].Value = v.Name;
                sheet.Cells[row, 2].Value = v.DataType.ToString();
                sheet.Cells[row, 3].Value = v.Address;
                sheet.Cells[row, 4].Value = v.StringLength;
            }

            sheet.Column(1).AutoFit();
            sheet.Column(2).AutoFit();
            sheet.Column(3).AutoFit();
            sheet.Column(4).AutoFit();

            package.SaveAs(new FileInfo(filePath));
        }

        #endregion

        #region 通讯属性

        /// <summary>
        /// 通讯实例（从输入连接器获取）
        /// </summary>
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

        /// <summary>是否已连接</summary>
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

        /// <summary>状态消息（IExecutableOperation 接口要求，保持简短不超出节点宽度）</summary>
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

        /// <summary>
        /// 输入信息（"当前数据" Tab 显示）
        /// 包含通讯实例详情 + inflag 状态（如启用）
        /// </summary>
        public string? InputInfo
        {
            get
            {
                if (CommunicationInstance == null && !IsInFlagEnabled)
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

                // inflag 信息
                if (IsInFlagEnabled)
                {
                    if (sb.Length > 0) sb.AppendLine();
                    var inflag = GetInFlagConnector();
                    var val = inflag?.Value?.ToString() ?? "null";
                    sb.Append($"{InFlagDataType} : InFlag = {val}");
                }

                return sb.Length > 0 ? sb.ToString() : null;
            }
        }

        /// <summary>
        /// 输出信息（"当前数据" Tab 显示，单行格式：[下标] 变量名 参数类型 值）
        /// 末尾显示 outflag 信息（如启用）
        /// </summary>
        public string? OutputInfo
        {
            get
            {
                if (_readVariables.Count == 0 && !IsOutFlagEnabled)
                    return null;

                var sb = new StringBuilder();
                for (int i = 0; i < _readVariables.Count; i++)
                {
                    var v = _readVariables[i];
                    var val = v.Value?.ToString() ?? "null";
                    sb.AppendLine($"[{i}] {v.Name} {v.DataType} {val}");
                }

                // outflag 信息
                if (IsOutFlagEnabled)
                {
                    var outflag = GetOutFlagConnector();
                    var val = outflag?.Value?.ToString() ?? "null";
                    sb.AppendLine($"{OutFlagDataType} : OutFlag = {val}");
                }

                return sb.ToString().TrimEnd();
            }
        }

        /// <summary>
        /// 节点内容区显示（简短信息，不超出节点宽度）
        /// </summary>
        public string InstanceInfo
        {
            get
            {
                if (CommunicationInstance == null)
                    return "未连接通讯实例";

                var info = $"连接: {CommunicationInstance.ConnectionString}\n";
                info += $"变量: {_readVariables.Count} 个";
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

        /// <summary>
        /// 执行读取操作
        /// 从输入连接器获取通讯实例，读取各变量地址的数据
        /// inflag 启用时判断是否允许直接获取实例，outflag 启用时输出执行结果
        /// </summary>
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

                // inflag 逻辑：判断是否允许直接获取实例
                if (IsInFlagEnabled && GetInFlagConnector()?.IsConnected == true)
                {
                    var inflagValue = GetInFlagConnector()!.Value;
                    // inflag 为 bool 类型且值为 false 时，阻止执行
                    if (inflagValue is bool inflagBool && !inflagBool)
                    {
                        RunStatusMessage(NodeRunStatus.Error, "InFlag 阻止执行");
                        SetOutFlagValue(false);
                        return false;
                    }
                    // inflag 为 null 时也阻止执行
                    if (inflagValue == null)
                    {
                        RunStatusMessage(NodeRunStatus.Error, "InFlag 无有效值");
                        SetOutFlagValue(false);
                        return false;
                    }
                }

                if (_readVariables.Count == 0)
                {
                    RunStatusMessage(NodeRunStatus.Error, "未配置读取变量");
                    SetOutFlagValue(false);
                    return false;
                }

                // 读取每个变量（通过 TCP 通讯实例读取数据）
                var successCount = 0;
                foreach (var variable in _readVariables)
                {
                    try
                    {
                        var result = ReadData(comm, variable);
                        variable.Value = result;
                        successCount++;

                        // 同步更新输出连接器的 Value
                        var idx = _readVariables.IndexOf(variable);
                        if (idx >= 0 && idx < Outputs.Count)
                            Outputs[idx].Value = result;
                    }
                    catch (Exception ex)
                    {
                        variable.Value = null;
                        System.Diagnostics.Debug.WriteLine(
                            $"[TcpsReadOperationViewModel] 读取变量 '{variable.Name}' 失败: {ex.Message}");
                    }
                }

                var allSuccess = successCount == _readVariables.Count;
                //StatusMessage = $"读取 {successCount}/{_readVariables.Count}";
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
                System.Diagnostics.Debug.WriteLine($"[TcpsReadOperationViewModel] Error: {ex.Message}");
                SetOutFlagValue(false);
                return false;
            }
        }

        /// <summary>设置 outflag 连接器的值（仅在启用时）</summary>
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

        /// <summary>通讯状态变化回调</summary>
        private void OnCommunicationStateChanged(object? sender, LYCorePro.Communacation.CommunicationState state)
        {
            IsConnected = state == LYCorePro.Communacation.CommunicationState.Connected;
        }

        /// <summary>
        /// 从通讯实例读取数据（虚方法，可被子类重写以支持不同协议）
        /// 优先使用协议特定的类型化读取方法，协议不支持时自动回退到接收原始数据后解析
        /// </summary>
        protected virtual object? ReadData(ICommunication comm, ReadVariable variable)
        {
            try
            {
                object? result = null;
                var task = Task.Run(async () =>
                {
                    try
                    {
                        // 优先尝试协议特定的类型化读取
                        result = variable.DataType switch
                        {
                            DataType.Bool => await comm.ReadAsync<bool>(variable.Address),
                            DataType.Int16 => await comm.ReadAsync<short>(variable.Address),
                            DataType.UInt16 => await comm.ReadAsync<ushort>(variable.Address),
                            DataType.Int32 => await comm.ReadAsync<int>(variable.Address),
                            DataType.UInt32 => await comm.ReadAsync<uint>(variable.Address),
                            DataType.Int64 => await comm.ReadAsync<long>(variable.Address),
                            DataType.UInt64 => await comm.ReadAsync<ulong>(variable.Address),
                            DataType.Float => await comm.ReadAsync<float>(variable.Address),
                            DataType.Double => await comm.ReadAsync<double>(variable.Address),
                            DataType.String => await comm.ReadStringAsync(variable.Address, variable.StringLength),
                            _ => null
                        };
                    }
                    catch (NotSupportedException)
                    {
                        // 协议不支持类型化读取（HTTP/WebSocket），回退到接收原始数据后解析
                        System.Diagnostics.Debug.WriteLine(
                            $"[ReadData] {comm.GetType().Name} 不支持类型化读取，回退到 ReceiveAsync");
                        var raw = await comm.ReceiveAsync();
                        result = ParseResponse(raw, variable.DataType);
                    }
                });

                if (task.Wait(TimeSpan.FromMilliseconds(5000)))
                    return result;

                return null;
            }
            catch
            {
                return null;
            }
        }

        /// <summary>解析响应数据为目标类型</summary>
        private static object? ParseResponse(string? response, DataType dataType)
        {
            if (string.IsNullOrEmpty(response)) return null;

            return dataType switch
            {
                DataType.Bool => bool.TryParse(response, out var b) ? b : null,
                DataType.Int16 => short.TryParse(response, out var i16) ? i16 : null,
                DataType.UInt16 => ushort.TryParse(response, out var u16) ? u16 : null,
                DataType.Int32 => int.TryParse(response, out var i32) ? i32 : null,
                DataType.UInt32 => uint.TryParse(response, out var u32) ? u32 : null,
                DataType.Int64 => long.TryParse(response, out var i64) ? i64 : null,
                DataType.UInt64 => ulong.TryParse(response, out var u64) ? u64 : null,
                DataType.Float => float.TryParse(response, out var f) ? f : null,
                DataType.Double => double.TryParse(response, out var d) ? d : null,
                DataType.String => response,
                DataType.Json => response,
                _ => response
            };
        }

        #endregion


        #region ISerializableOperation 实现

        /// <summary>序列化版本号（兼容不同版本的反序列化）</summary>
        private const int SerializeVersion = 3;

        /// <summary>
        /// 将插件参数序列化为二进制
        /// 格式：Version(int) → Title(string) → ReadVariables.Count(int) →
        ///       每个变量: Name(string) → DataType(int) → Address(string) → StringLength(ushort) →
        ///       IsInFlagEnabled(bool) → InFlagDataType(int) → IsOutFlagEnabled(bool) → OutFlagDataType(int)
        /// </summary>
        public void Serialize(BinaryWriter writer)
        {
            writer.Write(SerializeVersion);
            writer.Write(Title);
            writer.Write(_readVariables.Count);
            foreach (var v in _readVariables)
            {
                writer.Write(v.Name);
                writer.Write((int)v.DataType);
                writer.Write(v.Address);
                writer.Write(v.StringLength);
            }
            // v2: inflag / outflag
            writer.Write(IsInFlagEnabled);
            writer.Write((int)InFlagDataType);
            writer.Write(IsOutFlagEnabled);
            writer.Write((int)OutFlagDataType);
        }

        /// <summary>
        /// 从二进制反序列化插件参数
        /// </summary>
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
                var strLen = version >= 3 ? reader.ReadUInt16() : (ushort)256;

                AddVariableInternal(name, dataType, address, strLen);
            }

            // v2+: 恢复 inflag / outflag 状态
            if (version >= 2)
            {
                _isInFlagEnabled = reader.ReadBoolean();
                _inFlagDataType = (DataType)reader.ReadInt32();
                _isOutFlagEnabled = reader.ReadBoolean();
                _outFlagDataType = (DataType)reader.ReadInt32();

                // 触发连接器同步
                if (_isInFlagEnabled) SyncInFlagConnector();
                if (_isOutFlagEnabled) SyncOutFlagConnector();

                RaisePropertyChanged(nameof(IsInFlagEnabled));
                RaisePropertyChanged(nameof(InFlagDataType));
                RaisePropertyChanged(nameof(IsOutFlagEnabled));
                RaisePropertyChanged(nameof(OutFlagDataType));
            }
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
        /// 用于 inflag/outflag 数据类型变更时自动断开已有连线
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
                $"[TcpsReadOperationViewModel] 已断开 '{connector.Title}' 上的 {toRemove.Count} 条连线");
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
            if (_canExecute == null)
                return true;

            if (parameter == null)
                return _canExecute(default!);

            if (parameter is T t)
                return _canExecute(t);

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