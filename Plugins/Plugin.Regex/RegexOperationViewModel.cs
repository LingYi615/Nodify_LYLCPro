using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Threading;
using LYCorePro.Common.Helper;
using LYCorePro.Core;
using Nodify;
using PluginBase.Interfaces;
using PluginBase.Models;

namespace RegexPlugin
{
    /// <summary>
    /// 正则表达式插件操作（节点实例）
    /// 输入：一个可自定义数据类型的连接器
    /// 输出：一个可自定义数据类型的连接器
    /// 支持全部 Regex 语法、修饰符、元字符
    /// </summary>
    public class RegexOperationViewModel : NotifyPropertyBase, IExecutableOperation, ISerializableOperation,IConnectionAware
    {
        #region 字段

        private string _title = "正则表达式";
        private string _regexPattern = "";
        private string _replacementPattern = "";

        private string _icon = "M939.52 465.92C919.552 465.92 903.68 481.792 903.68 501.76V762.88C903.68 794.112 878.592 819.2 847.36 819.2H181.76C150.528 819.2 125.44 794.112 125.44 762.88V502.784C125.44 482.816 109.568 466.944 89.6 466.944S53.76 482.816 53.76 502.784V762.88C53.76 833.536 111.104 890.88 181.76 890.88H847.36C918.016 890.88 975.36 833.536 975.36 762.88V501.76C975.36 481.792 959.488 465.92 939.52 465.92ZM189.952 653.312C189.952 673.28 200.704 683.52 222.72 683.52C242.688 683.52 253.984 673.28 255.472 653.312V443.392L465.392 671.232C473.584 679.424 483.312 683.008 495.6 683.008C513.52 683.008 523.768 675.84 525.824 661.92C525.824 651.68 521.728 641.92 513.952 631.68L339.968 440.32H370.176C465.92 436.736 515.072 384.512 517.12 284.672C508.928 196.608 454.144 147.456 352.224 137.728H225.792C199.68 137.728 187.904 149.504 189.952 173.504V653.312ZM256 194.56H349.184C411.136 198.656 445.44 229.472 451.072 287.872C453.12 358.016 418.816 391.808 349.184 389.76H256V194.56ZM622.08 683.52H817.152C837.12 683.52 848.416 673.28 849.92 653.312C847.872 633.344 837.12 622.08 817.152 620.544H652.288V440.32H802.304C822.272 440.32 833.568 430.08 849.92 410.112C847.872 390.144 837.12 378.88 802.304 377.344H652.288V200.704H817.152C837.12 200.704 848.416 189.952 849.92 167.936C847.872 150.016 837.12 139.776 817.152 137.728H622.08C595.968 137.728 584.192 149.504 586.24 173.504V647.616C583.68 671.68 595.968 683.52 622.08 683.52Z";

        // IConnectionAware 实现
        private ObservableCollection<ConnectionViewModel>? _connections;


        private RegexOperationMode _operationMode = RegexOperationMode.Match;

        // 输入/输出数据类型
        private DataType _inputDataType = DataType.String;
        private DataType _outputDataType = DataType.String;

        // 修饰符
        private bool _ignoreCase;
        private bool _multiline;
        private bool _singleline;
        private bool _explicitCapture;
        private bool _ignorePatternWhitespace;
        private bool _rightToLeft;
        private bool _ecmaScript;
        private bool _cultureInvariant;
        private bool _compiled;

        private string? _statusMessage;
        private NodeRunStatus _runStatus;

        private string? _lastResult;

        #endregion

        #region 静态资源

        /// <summary>所有数据类型选项（供 ComboBox 绑定）</summary>
        public static DataType[] AllDataTypes { get; } = Enum
            .GetValues(typeof(DataType))
            .Cast<DataType>()
            .ToArray();

        /// <summary>操作模式选项</summary>
        public static RegexOperationMode[] OperationModes { get; } = Enum
            .GetValues(typeof(RegexOperationMode))
            .Cast<RegexOperationMode>()
            .ToArray();

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

        /// <summary>输入连接器（可自定义数据类型）</summary>
        public ObservableCollection<ConnectorViewModel> Inputs { get; } = new()
        {
            new ConnectorViewModel
            {
                Title = "输入",
                DataType = DataType.String,
                Direction = ConnectorDirection.Input
            }
        };

        /// <summary>输出连接器（可自定义数据类型）</summary>
        public ObservableCollection<ConnectorViewModel> Outputs { get; } = new()
        {
            new ConnectorViewModel
            {
                Title = "输出",
                DataType = DataType.String,
                Direction = ConnectorDirection.Output
            }
        };

        #endregion

        #region 正则参数属性

        /// <summary>正则表达式模式</summary>
        public string RegexPattern
        {
            get => _regexPattern;
            set
            {
                SetProperty(ref _regexPattern, value); RaisePropertyChanged(nameof(PatternPreview));
            }
        }

        /// <summary>替换字符串（Replace 模式使用，支持 $1 $2 等反向引用）</summary>
        public string ReplacementPattern
        {
            get => _replacementPattern;
            set
            {
                SetProperty(ref _replacementPattern, value); RaisePropertyChanged(nameof(PatternPreview));
            }
        }

        /// <summary>操作模式</summary>
        public RegexOperationMode OperationMode
        {
            get => _operationMode;
            set
            {
                SetProperty(ref _operationMode, value); 
                RaisePropertyChanged(nameof(IsReplacementVisible));
                RaisePropertyChanged(nameof(PatternPreview));
            }
        }

        /// <summary>输入数据类型</summary>
        public DataType InputDataType
        {
            get => _inputDataType;
            set
            {
                if (_inputDataType == value) return;
                if (Inputs.Count > 0)
                {
                    DisconnectConnector(Inputs[0]);
                    Inputs[0].DataType = value;
                }
                _inputDataType = value;
                if (Inputs.Count > 0) Inputs[0].DataType = value;
                RaisePropertyChanged(nameof(InputDataType));
                RaisePropertyChanged(nameof(InputInfo));
            }
        }

        /// <summary>输出数据类型</summary>
        public DataType OutputDataType
        {
            get => _outputDataType;
            set
            {
                if (_outputDataType == value) return;
                if (Outputs.Count > 0)
                {
                    DisconnectConnector(Outputs[0]);
                    Outputs[0].DataType = value;
                }
                _outputDataType = value;
                if (Outputs.Count > 0) Outputs[0].DataType = value;
                RaisePropertyChanged(nameof(OutputDataType));
                RaisePropertyChanged(nameof(OutputInfo));
            }
        }

        #endregion

        #region 修饰符属性

        /// <summary>忽略大小写 (i)</summary>
        public bool IgnoreCase
        {
            get => _ignoreCase;
            set {SetProperty(ref _ignoreCase , value); RaisePropertyChanged(nameof(ModifierFlags)); }
        }

        /// <summary>多行模式 (m) — ^$ 匹配每行首尾</summary>
        public bool Multiline
        {
            get => _multiline;
            set { SetProperty(ref _multiline, value); RaisePropertyChanged(nameof(ModifierFlags)); }
        }

        /// <summary>单行模式 (s) — . 匹配换行符</summary>
        public bool Singleline
        {
            get => _singleline;
            set { SetProperty(ref _singleline, value); RaisePropertyChanged(nameof(ModifierFlags)); }
        }

        /// <summary>仅显式捕获 (n) — 括号默认不捕获</summary>
        public bool ExplicitCapture
        {
            get => _explicitCapture;
            set { SetProperty(ref _explicitCapture, value); RaisePropertyChanged(nameof(ModifierFlags)); }
        }

        /// <summary>忽略模式空白 (x) — 忽略未转义空格和注释</summary>
        public bool IgnorePatternWhitespace
        {
            get => _ignorePatternWhitespace;
            set { SetProperty(ref _ignorePatternWhitespace, value); RaisePropertyChanged(nameof(ModifierFlags)); }
        }

        /// <summary>从右到左匹配</summary>
        public bool RightToLeft
        {
            get => _rightToLeft;
            set { SetProperty(ref _rightToLeft, value); RaisePropertyChanged(nameof(ModifierFlags)); }
        }

        /// <summary>ECMAScript 兼容模式</summary>
        public bool ECMAScript
        {
            get => _ecmaScript;
            set { SetProperty(ref _ecmaScript, value); RaisePropertyChanged(nameof(ModifierFlags)); }
        }

        /// <summary>固定区域性比较</summary>
        public bool CultureInvariant
        {
            get => _cultureInvariant;
            set { SetProperty(ref _cultureInvariant, value); RaisePropertyChanged(nameof(ModifierFlags)); }
        }

        /// <summary>编译为 IL（更快但初始化更慢）</summary>
        public bool Compiled
        {
            get => _compiled;
            set { SetProperty(ref _compiled, value); RaisePropertyChanged(nameof(ModifierFlags)); }
        }

        /// <summary>当前修饰符组合的字符串表示（如 "imns"）</summary>
        public string ModifierFlags
        {
            get
            {
                var sb = new StringBuilder();
                if (IgnoreCase) sb.Append('i');
                if (Multiline) sb.Append('m');
                if (Singleline) sb.Append('s');
                if (ExplicitCapture) sb.Append('n');
                if (IgnorePatternWhitespace) sb.Append('x');
                if (RightToLeft) sb.Append('r');
                if (ECMAScript) sb.Append('e');
                if (CultureInvariant) sb.Append('c');
                if (Compiled) sb.Append('C');
                return sb.Length > 0 ? sb.ToString() : "无";
            }
        }

        #endregion

        #region 显示属性

        /// <summary>是否显示替换输入框（仅 Replace 模式）</summary>
        public bool IsReplacementVisible => OperationMode == RegexOperationMode.Replace;

        /// <summary>状态消息</summary>
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

        /// <summary>模式预览</summary>
        public string PatternPreview
        {
            get
            {
                if (string.IsNullOrEmpty(RegexPattern)) return "未输入正则表达式";
                var mode = OperationMode switch
                {
                    RegexOperationMode.IsMatch => "是否匹配",
                    RegexOperationMode.Match => "匹配第一个",
                    RegexOperationMode.Matches => "匹配全部",
                    RegexOperationMode.Replace => "替换",
                    RegexOperationMode.Split => "分割",
                    _ => "未知"
                };
                var flags = ModifierFlags != "无" ? $" /{ModifierFlags}" : "";
                return $"/{RegexPattern}/{flags}  [{mode}]";
            }
        }

        /// <summary>节点内容区显示</summary>
        public string InstanceInfo
        {
            get
            {
                var sb = new StringBuilder();
                sb.AppendLine(PatternPreview);
                sb.Append($"输入: {InputDataType} → 输出: {OutputDataType}");
                if (!string.IsNullOrEmpty(StatusMessage))
                    sb.Append($"\n{StatusMessage}");
                return sb.ToString();
            }
        }

        /// <summary>输入信息</summary>
        public string? InputInfo
        {
            get
            {
                if (Inputs.Count == 0 || Inputs[0].Value == null) return null;
                var val = Inputs[0].Value;
                return $"类型: {InputDataType}\n值: {val}";
            }
        }

        /// <summary>输出信息</summary>
        public string? OutputInfo
        {
            get
            {
                if (Outputs.Count == 0 || Outputs[0].Value == null) return null;
                var val = Outputs[0].Value;
                if (val is string[] arr)
                    return $"类型: {OutputDataType}[]\n" + string.Join("\n", arr.Select((s, i) => $"[{i}] {s}"));
                return $"类型: {OutputDataType}\n值: {val}";
            }
        }

        /// <summary>上次执行结果文本</summary>
        public string? LastResult
        {
            get => _lastResult;
            set { SetProperty(ref _lastResult ,value); }
        }

        #endregion

        #region 构建 RegexOptions

        private RegexOptions BuildRegexOptions()
        {
            var options = RegexOptions.None;
            if (IgnoreCase) options |= RegexOptions.IgnoreCase;
            if (Multiline) options |= RegexOptions.Multiline;
            if (Singleline) options |= RegexOptions.Singleline;
            if (ExplicitCapture) options |= RegexOptions.ExplicitCapture;
            if (IgnorePatternWhitespace) options |= RegexOptions.IgnorePatternWhitespace;
            if (RightToLeft) options |= RegexOptions.RightToLeft;
            if (ECMAScript) options |= RegexOptions.ECMAScript;
            if (CultureInvariant) options |= RegexOptions.CultureInvariant;
            if (Compiled) options |= RegexOptions.Compiled;
            return options;
        }

        #endregion

        #region IExecutableOperation 实现

        public bool Execute()
        {
            RunStatusMessage(NodeRunStatus.Disabled, string.Empty);
            try
            {
                if (string.IsNullOrEmpty(RegexPattern))
                {
                    RunStatusMessage(NodeRunStatus.Error, "正则表达式为空");
                    Outputs[0].Value = null;
                    return false;
                }

                // 获取输入值并转为字符串
                var inputValue = Inputs.Count > 0 ? Inputs[0].Value : null;
                var inputString = inputValue?.ToString() ?? "";

                var options = BuildRegexOptions();
                var regex = new Regex(RegexPattern, options);

                object? result = OperationMode switch
                {
                    RegexOperationMode.IsMatch => regex.IsMatch(inputString),
                    RegexOperationMode.Match => regex.Match(inputString).Value,
                    RegexOperationMode.Matches => regex.Matches(inputString)
                        .Cast<Match>()
                        .Select(m => m.Value)
                        .ToArray(),
                    RegexOperationMode.Replace => regex.Replace(inputString, ReplacementPattern ?? ""),
                    RegexOperationMode.Split => regex.Split(inputString),
                    _ => null
                };

                // 尝试转换输出类型
                result = ConvertToOutputType(result);
                Outputs[0].Value = result;

                var count = result switch
                {
                    bool b => b ? "匹配" : "不匹配",
                    string s => s.Length > 50 ? s[..50] + "..." : s,
                    string[] arr => $"{arr.Length} 项",
                    _ => result?.ToString() ?? "null"
                };
                //StatusMessage = $"结果: {count}";
                LastResult = result?.ToString();

                RaisePropertyChanged(nameof(InputInfo));
                RaisePropertyChanged(nameof(OutputInfo));
                RaisePropertyChanged(nameof(InstanceInfo));
                return true;
            }
            catch (RegexParseException ex)
            {
                RunStatusMessage(NodeRunStatus.Error, $"正则语法错误: {ex.Message}");
                Outputs[0].Value = null;
                return false;
            }
            catch (Exception ex)
            {
                RunStatusMessage(NodeRunStatus.Error, $"异常: {ex.Message}");
                Outputs[0].Value = null;
                return false;
            }
        }

        /// <summary>将结果转换为目标数据类型</summary>
        private object? ConvertToOutputType(object? result)
        {
            if (result == null) return null;

            return OutputDataType switch
            {
                DataType.Bool => result is bool b ? b : result is string s && bool.TryParse(s, out var bv) ? bv : result is string[] arr && arr.Length > 0 && bool.TryParse(arr[0], out var bv2) ? bv2 : result.ToString()?.ToLower() == "true",
                DataType.Int16 => result is string str && int.TryParse(str, out var iv) ? iv : (result is string[] arr2 && arr2.Length > 0 && int.TryParse(arr2[0], out var iv2) ? iv2 : Convert.ToInt32(result)),
                DataType.Double => result is string str3 && double.TryParse(str3, out var dv) ? dv : (result is string[] arr3 && arr3.Length > 0 && double.TryParse(arr3[0], out var dv2) ? dv2 : Convert.ToDouble(result)),
                DataType.String => result is string[] arr4 ? string.Join(", ", arr4) : result?.ToString(),
                DataType.Json => result is string[] arr5 ? Newtonsoft.Json.JsonConvert.SerializeObject(arr5) : Newtonsoft.Json.JsonConvert.SerializeObject(result),
                _ => result is string[] arr6 ? string.Join(", ", arr6) : result?.ToString()
            };
        }

        #endregion

        #region ISerializableOperation 实现

        private const int SerializeVersion = 1;

        public void Serialize(BinaryWriter writer)
        {
            writer.Write(SerializeVersion);
            writer.Write(Title);
            writer.Write(RegexPattern ?? "");
            writer.Write(ReplacementPattern ?? "");
            writer.Write((int)OperationMode);
            writer.Write((int)InputDataType);
            writer.Write((int)OutputDataType);
            writer.Write(IgnoreCase);
            writer.Write(Multiline);
            writer.Write(Singleline);
            writer.Write(ExplicitCapture);
            writer.Write(IgnorePatternWhitespace);
            writer.Write(RightToLeft);
            writer.Write(ECMAScript);
            writer.Write(CultureInvariant);
            writer.Write(Compiled);
        }

        public void Deserialize(BinaryReader reader)
        {
            var version = reader.ReadInt32();
            Title = reader.ReadString();
            RegexPattern = reader.ReadString();
            ReplacementPattern = reader.ReadString();
            _operationMode = (RegexOperationMode)reader.ReadInt32();
            _inputDataType = (DataType)reader.ReadInt32();
            _outputDataType = (DataType)reader.ReadInt32();
            _ignoreCase = reader.ReadBoolean();
            _multiline = reader.ReadBoolean();
            _singleline = reader.ReadBoolean();
            _explicitCapture = reader.ReadBoolean();
            _ignorePatternWhitespace = reader.ReadBoolean();
            _rightToLeft = reader.ReadBoolean();
            _ecmaScript = reader.ReadBoolean();
            _cultureInvariant = reader.ReadBoolean();
            _compiled = reader.ReadBoolean();

            // 同步连接器数据类型
            if (Inputs.Count > 0) Inputs[0].DataType = _inputDataType;
            if (Outputs.Count > 0) Outputs[0].DataType = _outputDataType;

            RaisePropertyChanged(nameof(OperationMode));
            RaisePropertyChanged(nameof(InputDataType));
            RaisePropertyChanged(nameof(OutputDataType));
            RaisePropertyChanged(nameof(IgnoreCase));
            RaisePropertyChanged(nameof(Multiline));
            RaisePropertyChanged(nameof(Singleline));
            RaisePropertyChanged(nameof(ExplicitCapture));
            RaisePropertyChanged(nameof(IgnorePatternWhitespace));
            RaisePropertyChanged(nameof(RightToLeft));
            RaisePropertyChanged(nameof(ECMAScript));
            RaisePropertyChanged(nameof(CultureInvariant));
            RaisePropertyChanged(nameof(Compiled));
            RaisePropertyChanged(nameof(PatternPreview));
            RaisePropertyChanged(nameof(IsReplacementVisible));
            RaisePropertyChanged(nameof(ModifierFlags));
            RaisePropertyChanged(nameof(InstanceInfo));
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
                $"[RegexOperationViewModel] 已断开 '{connector.Title}' 上的 {toRemove.Count} 条连线");
        }

        #endregion

    }

    /// <summary>
    /// 正则表达式操作模式
    /// </summary>
    public enum RegexOperationMode
    {
        /// <summary>是否匹配 — 返回 bool</summary>
        IsMatch,
        /// <summary>匹配第一个 — 返回 string</summary>
        Match,
        /// <summary>匹配全部 — 返回 string[]</summary>
        Matches,
        /// <summary>替换 — 返回 string</summary>
        Replace,
        /// <summary>分割 — 返回 string[]</summary>
        Split
    }
}