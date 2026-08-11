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
using OfficeOpenXml.FormulaParsing.Excel.Functions.Text;
using PluginBase.Interfaces;
using PluginBase.Models;

namespace ParameterEnactmentPlugin
{
    /// <summary>
    ///参数设定
    /// 输出：动态注册的变量列表，每个变量对应一个输出连接器
    /// 实现 IExecutableOperation 接口，点击执行按钮时自动调用 Execute()
    /// </summary>
    public class ParameterEnactmentOperationViewModel : NotifyPropertyBase, IExecutableOperation, ISerializableOperation, IConnectionAware
    {
        #region 字段

        private string _title = "参数设定";
        private string? _statusMessage;
        private NodeRunStatus _runStatus; 


        private string _icon = "M768 128H256C185.6 128 128 185.6 128 256v512c0 70.4 57.6 128 128 128h128c19.2 0 32-12.8 32-32s-12.8-32-32-32H256c-35.2 0-64-28.8-64-64V256c0-35.2 28.8-64 64-64h512c35.2 0 64 28.8 64 64v160c0 19.2 12.8 32 32 32s32-12.8 32-32V256c0-70.4-57.6-128-128-128z M864 544h-160c-12.8-38.4-48-64-89.6-64-41.6 0-76.8 25.6-89.6 64h-38.4c-19.2 0-32 12.8-32 32s12.8 32 32 32h38.4c12.8 38.4 48 64 89.6 64 41.6 0 76.8-28.8 89.6-64h160c19.2 0 32-12.8 32-32s-12.8-32-32-32z m-246.4 64c-19.2 0-32-12.8-32-32s12.8-32 32-32 32 12.8 32 32-16 32-32 32zM864 768h-38.4c-12.8-38.4-48-64-89.6-64-41.6 0-76.8 28.8-89.6 64h-160c-19.2 0-32 12.8-32 32s12.8 32 32 32h160c12.8 38.4 48 64 89.6 64 41.6 0 76.8-25.6 89.6-64H864c19.2 0 32-12.8 32-32s-12.8-32-32-32z m-128 64c-19.2 0-32-12.8-32-32s12.8-32 32-32 32 12.8 32 32-12.8 32-32 32zM736 320c0-19.2-12.8-32-32-32H320c-19.2 0-32 12.8-32 32s12.8 32 32 32h384c19.2 0 32-12.8 32-32zM384 480h-64c-19.2 0-32 12.8-32 32s12.8 32 32 32h64c19.2 0 32-12.8 32-32s-12.8-32-32-32zM384 672h-64c-19.2 0-32 12.8-32 32s12.8 32 32 32h64c19.2 0 32-12.8 32-32s-12.8-32-32-32z";


        // IConnectionAware 实现
        private ObservableCollection<ConnectionViewModel>? _connections;

        // 参数设定列表（用户可动态增删）
        private readonly ObservableCollection<EnactmentVariable> _enactmentVariables = new();

        #endregion

        #region 命令

        /// <summary>添加变量命令</summary>
        public ICommand AddVariableCommand { get; }

        /// <summary>移除变量命令（参数为 EnactmentVariable）</summary>
        public ICommand RemoveVariableCommand { get; }

        /// <summary>从 CSV/Excel 导入变量命令</summary>
        public ICommand ImportVariablesCommand { get; }

        /// <summary>导出变量到 CSV 命令</summary>
        public ICommand ExportVariablesCommand { get; }

        /// <summary>构造函数</summary>
        public ParameterEnactmentOperationViewModel()
        {
            AddVariableCommand = new RelayCommand<object?>(_ => AddVariable());
            RemoveVariableCommand = new RelayCommand<EnactmentVariable>(RemoveVariable);
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


        /// <summary>输出连接器（动态生成变量输出 + 可选 outflag）</summary>
        public ObservableCollection<ConnectorViewModel> Outputs { get; } = new();

        #endregion



        #region 变量管理

        /// <summary>读取变量集合（参数面板绑定）</summary>
        public ObservableCollection<EnactmentVariable> EnactmentVariables => _enactmentVariables;

        /// <summary>添加一个读取变量，并同步创建对应的输出连接器</summary>
        public void AddVariable()
        {
            var variable = new EnactmentVariable
            {
                Name = $"参数{_enactmentVariables.Count + 1}",
                DataType = DataType.String,
                ParameterValue = $"{_enactmentVariables.Count}"
            };
            variable.PropertyChanged += OnVariablePropertyChanged;
            _enactmentVariables.Add(variable);

            var connector = new ConnectorViewModel
            {
                Title = variable.Name,
                DataType = variable.DataType,
                Direction = ConnectorDirection.Output
            };

            Outputs.Add(connector);

            RaisePropertyChanged(nameof(OutputInfo));
        }

        /// <summary>移除指定变量，并同步移除对应的输出连接器及其连线</summary>
        public void RemoveVariable(EnactmentVariable variable)
        {
            variable.PropertyChanged -= OnVariablePropertyChanged;

            // 找到对应的输出连接器，先断开连线再移除
            var idx = _enactmentVariables.IndexOf(variable);
            _enactmentVariables.Remove(variable);

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
            if (sender is not EnactmentVariable variable) return;
            var idx = _enactmentVariables.IndexOf(variable);
            if (idx < 0 || idx >= Outputs.Count) return;

            var connector = Outputs[idx];
            switch (e.PropertyName)
            {
                case nameof(EnactmentVariable.Name):
                    connector.Title = variable.Name;
                    break;
                case nameof(EnactmentVariable.DataType):
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
                System.Diagnostics.Debug.WriteLine($"[ParameterEnactmentOperationViewModel] 导入失败: {ex.Message}");
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

                var value = parts[2].Trim();

                // 类型校验：值与数据类型不一致时置空
                if (!IsValidValueForType(value, dataType))
                {
                    value = string.Empty;
                }

                AddVariableInternal(name, dataType, value);
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

                var parameterValue = sheet.Cells[row, 3].Text.Trim();
                // 类型校验：值与数据类型不一致时置空
                if (!IsValidValueForType(parameterValue, dataType))
                {
                    parameterValue = string.Empty;
                }

                AddVariableInternal(name, dataType, parameterValue);
            }
        }

        /// <summary>内部方法：创建变量和对应的输出连接器</summary>
        private void AddVariableInternal(string name, DataType dataType, string parameterValue)
        {
            var variable = new EnactmentVariable
            {
                Name = name,
                DataType = dataType,
                ParameterValue = parameterValue
            };
            variable.PropertyChanged += OnVariablePropertyChanged;
            _enactmentVariables.Add(variable);

            var connector = new ConnectorViewModel
            {
                Title = variable.Name,
                DataType = variable.DataType,
                Direction = ConnectorDirection.Output
            };
            Outputs.Add(connector);
        }

        /// <summary>
        /// 导出变量到 CSV 或 Excel 文件（默认 CSV）
        /// </summary>
        private void ExportVariables()
        {
            if (_enactmentVariables.Count == 0) return;

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
                System.Diagnostics.Debug.WriteLine($"[ParameterEnactmentOperationViewModel] 导出失败: {ex.Message}");
            }
        }

        /// <summary>导出到 CSV 文件</summary>
        private void ExportToCsv(string filePath)
        {
            var lines = _enactmentVariables.Select(v =>
                 $"{v.Name},{v.DataType},{v.ParameterValue}");
            File.WriteAllLines(filePath, lines);
        }

        /// <summary>导出到 Excel (.xlsx) 文件（使用 EPPlus）</summary>
        private void ExportToExcel(string filePath)
        {

            using var package = new ExcelPackage();
            var sheet = package.Workbook.Worksheets.Add("参数设定列表");

            // 表头
            sheet.Cells[1, 1].Value = "名称";
            sheet.Cells[1, 2].Value = "类型";
            sheet.Cells[1, 3].Value = "值";

            // 表头样式
            using (var headerRange = sheet.Cells[1, 1, 1, 4])
            {
                headerRange.Style.Font.Bold = true;
                headerRange.Style.Fill.PatternType = OfficeOpenXml.Style.ExcelFillStyle.Solid;
                headerRange.Style.Fill.BackgroundColor.SetColor(System.Drawing.Color.FromArgb(49, 50, 68));
                headerRange.Style.Font.Color.SetColor(System.Drawing.Color.FromArgb(137, 180, 250));
            }

            // 数据行
            for (int i = 0; i < _enactmentVariables.Count; i++)
            {
                var v = _enactmentVariables[i];
                var row = i + 2;
                sheet.Cells[row, 1].Value = v.Name;
                sheet.Cells[row, 2].Value = v.DataType.ToString();
                sheet.Cells[row, 3].Value = v.ParameterValue;
            }

            sheet.Column(1).AutoFit();
            sheet.Column(2).AutoFit();
            sheet.Column(3).AutoFit();
            sheet.Column(4).AutoFit();

            package.SaveAs(new FileInfo(filePath));
        }

        #endregion

        #region 通讯属性

       

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
        /// 输出信息（"当前数据" Tab 显示，单行格式：[下标] 变量名 参数类型 值）
        /// </summary>
        public string? OutputInfo
        {
            get
            {
                if (_enactmentVariables.Count == 0)
                    return null;

                var sb = new StringBuilder();
                for (int i = 0; i < _enactmentVariables.Count; i++)
                {
                    var v = _enactmentVariables[i];
                    var val = v.Value?.ToString() ?? "null";
                    sb.AppendLine($"[{i}] {v.Name} {v.DataType} {val}");
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

                var info = $"变量: {_enactmentVariables.Count} 个";
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

                if (_enactmentVariables.Count == 0)
                {
                    RunStatusMessage(NodeRunStatus.Error, "未配置读取变量");
                    return false;
                }

                // 输出参数设定配置
                var successCount = 0;
                foreach (var variable in _enactmentVariables)
                {
                    try
                    {
                        successCount++;

                        var idx = _enactmentVariables.IndexOf(variable);
                        if (idx >= 0 && idx < Outputs.Count)
                        {
                            var connector = Outputs[idx];
                            // ==========增加类型校验转换==========
                            if (TryConvertToConnectorDataType(variable.ParameterValue, connector.DataType, out var convertedValue))
                            {
                                connector.Value = convertedValue;
                                variable.Value = connector.Value;
                            }
                            else
                            {
                                // 类型转换失败，清空值，记录日志
                                connector.Value = variable.Value = null;
                                System.Diagnostics.Debug.WriteLine(
                                    $"[ParameterEnactmentOperationViewModel]变量[{variable.Name}]值:{variable.ParameterValue} 无法转换为类型 {connector.DataType}");
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        variable.ParameterValue = null;
                        System.Diagnostics.Debug.WriteLine(
                            $"[ParameterEnactmentOperationViewModel] 读取变量 '{variable.Name}' 失败: {ex.Message}");
                    }
                }

                var allSuccess = successCount == _enactmentVariables.Count;
                //StatusMessage = $"读取 {successCount}/{_enactmentVariable.Count}";
                RaisePropertyChanged(nameof(OutputInfo));
                RaisePropertyChanged(nameof(InstanceInfo));
                //RunStatusMessage(NodeRunStatus.Completed, "执行成功");
                return allSuccess;
            }
            catch (Exception ex)
            {
                RunStatusMessage(NodeRunStatus.Error, $"异常: {ex.Message}");
                System.Diagnostics.Debug.WriteLine($"[ParameterEnactmentOperationViewModel] Error: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// 将输入值转换为连接器声明的DataType，校验是否兼容
        /// </summary>
        /// <param name="rawValue">原始参数值</param>
        /// <param name="targetDataType">连接器目标数据类型</param>
        /// <param name="result">转换成功返回转换后对象，失败返回null</param>
        /// <returns>true=转换成功，false=类型不匹配转换失败</returns>
        private static bool TryConvertToConnectorDataType(object? rawValue, DataType targetDataType, out object? result)
        {
            result = null;
            if (rawValue == null)
            {
                return true;
            }
            string? strVal = rawValue.ToString();
            if (string.IsNullOrEmpty(strVal))
            {
                return true;
            }

            switch (targetDataType)
            {
                case DataType.Bool:
                    if (bool.TryParse(strVal, out var bVal)) { result = bVal; return true; }
                    return false;
                case DataType.Int16:
                    if (short.TryParse(strVal, out var i16)) { result = i16; return true; }
                    return false;
                case DataType.UInt16:
                    if (ushort.TryParse(strVal, out var u16)) { result = u16; return true; }
                    return false;
                case DataType.Int32:
                    if (int.TryParse(strVal, out var i32)) { result = i32; return true; }
                    return false;
                case DataType.UInt32:
                    if (uint.TryParse(strVal, out var u32)) { result = u32; return true; }
                    return false;
                case DataType.Int64:
                    if (long.TryParse(strVal, out var i64)) { result = i64; return true; }
                    return false;
                case DataType.UInt64:
                    if (ulong.TryParse(strVal, out var u64)) { result = u64; return true; }
                    return false;
                case DataType.Float:
                    if (float.TryParse(strVal, out var f)) { result = f; return true; }
                    return false;
                case DataType.Double:
                    if (double.TryParse(strVal, out var d)) { result = d; return true; }
                    return false;
                case DataType.String:
                case DataType.Json:
                    result = strVal;
                    return true;
                default:
                    result = strVal;
                    return true;
            }
        }


        /// <summary>
        /// 校验值是否与指定数据类型匹配
        /// </summary>
        private static bool IsValidValueForType(string value, DataType dataType)
        {
            if (string.IsNullOrEmpty(value)) return true; // 空值视为有效

            switch (dataType)
            {
                case DataType.Bool:
                    return bool.TryParse(value, out _);
                case DataType.Int16:
                case DataType.UInt16:
                case DataType.Int32:
                case DataType.UInt32:
                case DataType.Int64:
                case DataType.UInt64:
                    return int.TryParse(value, out _);
                case DataType.Float:
                    return float.TryParse(value, out _);
                case DataType.Double:
                    return double.TryParse(value, out _);
                case DataType.String: 
                case DataType.Json:
                    return true;
                default:
                    return true;
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
        private const int SerializeVersion = 1;

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
            writer.Write(_enactmentVariables.Count);
            foreach (var v in _enactmentVariables)
            {
                writer.Write(v.Name);
                writer.Write((int)v.DataType);
                writer.Write((string)v.ParameterValue);
            }
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
                var parameterValue = reader.ReadString();

                AddVariableInternal(name, dataType, parameterValue);
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
                $"[ParameterEnactmentOperationViewModel] 已断开 '{connector.Title}' 上的 {toRemove.Count} 条连线");
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