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

namespace SerialReadPlugin
{
    /// <summary>
    /// 串口读取插件操作（节点实例）
    /// 输入：SerialAgreement（ICommunication 通讯实例）
    /// 输出：动态注册的变量列表，每个变量对应一个输出连接器
    /// </summary>
    public class SerialReadOperationViewModel : NotifyPropertyBase, IExecutableOperation, ISerializableOperation, IConnectionAware
    {
        #region 字段

        private string _title = "串口读取";
        private string? _statusMessage;
        private NodeRunStatus _runStatus;
        private ICommunication? _communicationInstance;
        private bool _isConnected;

        private bool _isInFlagEnabled;
        private bool _isOutFlagEnabled;
        private DataType _inFlagDataType = DataType.Bool;
        private DataType _outFlagDataType = DataType.Bool;

        private ObservableCollection<ConnectionViewModel>? _connections;
        private readonly ObservableCollection<ReadVariable> _readVariables = new();

        private string _icon = "M183.0912 179.456 L378.7776 369.664 L366.336 381.7984 L170.6496 191.5904 L183.0912 179.456 Z M665.3952 151.7568 L577.28 406.7328 L560.5888 401.3056 L648.6528 146.3296 L665.3952 151.7568 Z M874.6496 560.5888 L597.9648 561.7152 L597.8624 544.6144 L874.5984 543.488 L874.6496 560.5888 Z M665.3952 835.9424 L544.8192 593.7664 L560.6912 586.3424 L681.216 828.4672 L665.3952 835.9424 Z M352.5664 627.2 C340.0192 586.4448 320.0064 546.3296 320.0064 522.4448 C320.0064 418.56 405.9712 334.3872 512.0064 334.3872 C618.0416 334.3872 704.0064 418.56 704.0064 522.4448 C704.0064 626.3296 618.0416 710.5536 512.0064 710.5536 C454.9856 710.5536 400.2816 679.936 364.0192 639.024 L215.6928 759.344 C228.3392 777.6224 235.6608 799.6896 235.6608 823.4464 C235.6608 886.9344 183.1296 940.3904 118.3616 940.3904 C53.5904 940.3904 0 888.9344 0 825.4464 C0 761.9584 53.5904 709.5536 118.3616 709.5536 C152.6144 709.5536 183.3824 723.8896 204.8896 746.8288 L353.5264 626.2016 Z M0 114.944 C0 51.456 52.5312 0 117.3504 0 C182.1184 0 234.6496 51.456 234.6496 114.944 C234.6496 178.432 182.1184 229.888 117.3504 229.888 C52.5312 229.888 0 178.432 0 114.944 Z M554.6496 114.944 C554.6496 51.456 607.232 0 672 0 C736.8192 0 789.3504 51.456 789.3504 114.944 C789.3504 178.432 736.8192 229.888 672 229.888 C607.232 229.888 554.6496 178.432 554.6496 114.944 Z M789.3504 574.6688 C789.3504 511.232 841.8816 459.776 906.6496 459.776 C971.4688 459.776 1024 511.232 1024 574.6688 C1024 638.1568 971.4688 690.6144 906.6496 690.6144 C841.8816 690.6144 789.3504 638.1568 789.3504 574.6688 Z M597.3504 909.0048 C597.3504 845.5168 649.8816 794.0608 714.6496 794.0608 C779.4688 794.0608 832 845.5168 832 909.0048 C832 972.4928 779.4688 1024 714.6496 1024 C649.8816 1024 597.3504 972.4928 597.3504 909.0048 Z";


        #endregion

        #region 命令

        public ICommand AddVariableCommand { get; }
        public ICommand RemoveVariableCommand { get; }
        public ICommand ImportVariablesCommand { get; }
        public ICommand ExportVariablesCommand { get; }

        public SerialReadOperationViewModel()
        {
            AddVariableCommand = new RelayCommand<object?>(_ => AddVariable());
            RemoveVariableCommand = new RelayCommand<ReadVariable>(RemoveVariable);
            ImportVariablesCommand = new RelayCommand<object?>(_ => ImportVariables());
            ExportVariablesCommand = new RelayCommand<object?>(_ => ExportVariables());
        }

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

        public ObservableCollection<ConnectorViewModel> Inputs { get; } = new()
        {
            new ConnectorViewModel
            {
                Title = "Instance",
                DataType = DataType.SerialAgreement,
                Direction = ConnectorDirection.Input
            }
        };

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
                SetProperty(ref _inFlagDataType, value);
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
                SetProperty(ref _isOutFlagEnabled, value);
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
                SetProperty(ref _outFlagDataType,   value);
                
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

        private ConnectorViewModel? GetInFlagConnector()
        {
            return Inputs.Count > 1 ? Inputs[1] : null;
        }

        private ConnectorViewModel? GetOutFlagConnector()
        {
            return Outputs.Count > _readVariables.Count
                ? Outputs[^1]
                : null;
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
                if (Inputs.Count > 1)
                {
                    var inflag = Inputs[1];
                    DisconnectConnector(inflag);
                    Inputs.RemoveAt(1);
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
                if (Outputs.Count > _readVariables.Count)
                {
                    var outflag = Outputs[^1];
                    DisconnectConnector(outflag);
                    Outputs.RemoveAt(Outputs.Count - 1);
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

        public ObservableCollection<ReadVariable> ReadVariables => _readVariables;

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
            var insertIdx = Outputs.Count - (IsOutFlagEnabled ? 1 : 0);
            Outputs.Insert(insertIdx, connector);

            RaisePropertyChanged(nameof(OutputInfo));
        }

        public void RemoveVariable(ReadVariable variable)
        {
            variable.PropertyChanged -= OnVariablePropertyChanged;

            var idx = _readVariables.IndexOf(variable);
            _readVariables.Remove(variable);

            if (idx >= 0 && idx < Outputs.Count)
            {
                DisconnectConnector(Outputs[idx]);
                Outputs.RemoveAt(idx);
            }

            RaisePropertyChanged(nameof(OutputInfo));
        }

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
                System.Diagnostics.Debug.WriteLine($"[SerialReadOperationViewModel] 导入失败: {ex.Message}");
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

                ushort strLen = 256;
                if (parts.Length >= 4 && ushort.TryParse(parts[3].Trim(), out var len))
                    strLen = len;

                AddVariableInternal(name, dataType, parts[2].Trim(), strLen);
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
                ushort strLen = 256;
                if (ushort.TryParse(sheet.Cells[row, 4].Text.Trim(), out var len))
                    strLen = len;

                AddVariableInternal(name, dataType, address, strLen);
            }
        }

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
            var insertIdx = Outputs.Count - (IsOutFlagEnabled ? 1 : 0);
            Outputs.Insert(insertIdx, connector);
        }

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
                System.Diagnostics.Debug.WriteLine($"[SerialReadOperationViewModel] 导出失败: {ex.Message}");
            }
        }

        private void ExportToCsv(string filePath)
        {
            var lines = _readVariables.Select(v =>
                $"{v.Name},{v.DataType},{v.Address},{v.StringLength}");
            File.WriteAllLines(filePath, lines);
        }

        private void ExportToExcel(string filePath)
        {
            using var package = new ExcelPackage();
            var sheet = package.Workbook.Worksheets.Add("变量列表");

            sheet.Cells[1, 1].Value = "名称";
            sheet.Cells[1, 2].Value = "类型";
            sheet.Cells[1, 3].Value = "地址";
            sheet.Cells[1, 4].Value = "字符串长度";

            using (var headerRange = sheet.Cells[1, 1, 1, 4])
            {
                headerRange.Style.Font.Bold = true;
                headerRange.Style.Fill.PatternType = OfficeOpenXml.Style.ExcelFillStyle.Solid;
                headerRange.Style.Fill.BackgroundColor.SetColor(System.Drawing.Color.FromArgb(49, 50, 68));
                headerRange.Style.Font.Color.SetColor(System.Drawing.Color.FromArgb(137, 180, 250));
            }

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

                if (IsOutFlagEnabled)
                {
                    var outflag = GetOutFlagConnector();
                    var val = outflag?.Value?.ToString() ?? "null";
                    sb.AppendLine($"{OutFlagDataType} : OutFlag = {val}");
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
                if (Inputs.Count == 0 || Inputs[0].Value is not ICommunication comm)
                {
                    RunStatusMessage(NodeRunStatus.Error, "无通讯实例输入");
                    SetOutFlagValue(false);
                    return false;
                }

                CommunicationInstance = comm;

                comm.OnStateChanged += OnCommunicationStateChanged;
                IsConnected = comm.IsConnected;
                comm.OnStateChanged -= OnCommunicationStateChanged;

                if (!IsConnected)
                {
                    RunStatusMessage(NodeRunStatus.Error, "通讯未连接");
                    SetOutFlagValue(false);
                    return false;
                }

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

                if (_readVariables.Count == 0)
                {
                    RunStatusMessage(NodeRunStatus.Error, "未配置读取变量");
                    SetOutFlagValue(false);
                    return false;
                }

                var successCount = 0;
                foreach (var variable in _readVariables)
                {
                    try
                    {
                        var result = ReadData(comm, variable);
                        variable.Value = result;
                        successCount++;

                        var idx = _readVariables.IndexOf(variable);
                        if (idx >= 0 && idx < Outputs.Count)
                            Outputs[idx].Value = result;
                    }
                    catch (Exception ex)
                    {
                        variable.Value = null;
                        System.Diagnostics.Debug.WriteLine(
                            $"[SerialReadOperationViewModel] 读取变量 '{variable.Name}' 失败: {ex.Message}");
                    }
                }

                var allSuccess = successCount == _readVariables.Count;
                //StatusMessage = $"读取 {successCount}/{_readVariables.Count}";
                RaisePropertyChanged(nameof(InputInfo));
                RaisePropertyChanged(nameof(OutputInfo));
                RaisePropertyChanged(nameof(InstanceInfo));
                SetOutFlagValue(allSuccess);
                return allSuccess;
            }
            catch (Exception ex)
            {
                RunStatusMessage(NodeRunStatus.Error, $"异常: {ex.Message}");
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

        protected virtual object? ReadData(ICommunication comm, ReadVariable variable)
        {
            try
            {
                object? result = null;
                var task = Task.Run(async () =>
                {
                    try
                    {
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

        private const int SerializeVersion = 1;

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
                var strLen = reader.ReadUInt16();

                AddVariableInternal(name, dataType, address, strLen);
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
        }

        #endregion
    }

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