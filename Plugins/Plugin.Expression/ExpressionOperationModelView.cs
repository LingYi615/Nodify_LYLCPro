using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Input;
using System.Xml.Linq;
using LYCorePro.Common.Helper;
using LYCorePro.Core;
using Nodify;
using PluginBase.Interfaces;
using PluginBase.Models;

namespace ExpressionPlugin
{
    /// <summary>
    /// 表达式操作（节点实例）
    /// - 初始无输入/输出，支持动态添加
    /// - 输入/输出数据类型可自定义修改
    /// - 支持 Linq、三角函数、字符串处理等功能
    /// </summary>
    public class ExpressionOperationModelView : NotifyPropertyBase, IExecutableOperation, ISerializableOperation, IConnectionAware
    {
        #region 静态资源

        public static DataType[] AvailableDataTypes { get; } = Enum
            .GetValues(typeof(DataType))
            .Cast<DataType>()
            .Where(dt => dt != DataType.TCPAgreement && dt != DataType.Any)
            .ToArray();

        #endregion

        #region 字段

        private string _title = "表达式";
        private string? _statusMessage;
        private string _expression = "";
        private NodeRunStatus _runStatus;
        // IConnectionAware 实现
        private ObservableCollection<ConnectionViewModel>? _connections;

        private string _icon = "M780.651821 172.762532V31.993062h102.377797v140.76947H1023.799088v102.377797h-140.76947v140.769471h-102.377797V275.140329H639.88235V172.762532h140.769471zM502.2482 375.214625a40.631188 40.631188 0 0 1-8.702113 27.769978 32.376978 32.376978 0 0 1-25.274519 11.581488 26.426269 26.426269 0 0 1-22.907032-10.045821 136.930303 136.930303 0 0 1-14.268905-32.632923 42.102869 42.102869 0 0 0-6.398613-13.757017 16.124503 16.124503 0 0 0-12.157363-4.862945 15.228697 15.228697 0 0 0-13.948975 8.190224 83.18196 83.18196 0 0 0-8.382182 21.11542L345.738143 529.805099h60.72283l-10.493724 34.552506h-58.035413l-61.810595 213.521692c-13.245127 44.150425-28.857741 87.405044-46.70987 129.699872-12.797225 30.969284-31.609145 58.867233-55.220024 81.710279a126.564551 126.564551 0 0 1-91.116239 34.16859 91.308198 91.308198 0 0 1-60.978776-17.916115 53.108482 53.108482 0 0 1-22.075212-40.951119 43.318605 43.318605 0 0 1 11.325544-30.52138 36.919993 36.919993 0 0 1 29.0497-13.117156c11.133585-2.175528 22.395143 2.559445 29.049699 12.22135 4.670987 9.469946 7.550363 19.835698 8.382183 30.521381 0 16.764364 8.446168 25.338505 20.603531 25.338504a34.360548 34.360548 0 0 0 29.0497-21.11542c10.23778-20.475559 18.236045-42.230841 23.802838-64.625984l100.138282-338.67855h-62.7064l9.853863-34.168589h62.7064l6.718543-24.122769c7.678335-30.20145 17.276253-59.763039 28.985714-88.492808 12.285336-26.106338 29.817533-49.141342 51.380857-67.377387A118.374328 118.374328 0 0 1 430.839686 319.930615c11.517502 0.383917 22.907032 2.879376 33.656701 7.294418 10.23778 3.839167 19.387795 10.23778 26.682213 18.619962a45.046231 45.046231 0 0 1 9.917849 30.521381l1.151751-1.279723zM767.854596 570.500273c0.063986 15.484642-2.047556 30.905297-6.398612 45.750078h-33.912645c4.28707-15.036739 6.654557-30.713339 6.910501-46.389939a25.08256 25.08256 0 0 0-2.559445-10.685683 10.877641 10.877641 0 0 0-10.493724-5.502807 14.076947 14.076947 0 0 0-9.597919 3.391265c-6.974487 6.462598-13.565058 13.437086-19.707726 20.731504l-66.161651 72.94418 28.985714 99.178491c2.751403 9.150016 6.078682 18.108073 9.853863 26.810185 3.519237 7.038474 7.550363 10.365752 12.54128 10.365752 4.926931 0 15.612614-6.718543 22.331157-20.155629 5.758751-11.38953 10.55771-23.290949 14.204919-35.704256h31.993062a218.960513 218.960513 0 0 1-29.0497 66.545568 120.869786 120.869786 0 0 1-36.600062 37.495868 71.536486 71.536486 0 0 1-36.856007 11.901419 48.437495 48.437495 0 0 1-42.678744-20.47556 196.309425 196.309425 0 0 1-26.170325-57.58751l-13.629044-47.029801-76.015514 87.021127a90.348406 90.348406 0 0 1-58.0994 37.751813c-20.603532 0-31.03327-14.588836-31.033269-43.57455 0.255944-21.051434 4.479029-41.782938 12.477294-61.042761h38.327687a218.832541 218.832541 0 0 0-11.069599 48.501481c0 7.678335 2.047556 11.581488 6.718543 11.581488 4.607001 0 10.173794-4.862945 19.451781-14.588836l89.964489-101.609963-17.404225-58.291358a426.211565 426.211565 0 0 0-8.446168-26.23431 50.677009 50.677009 0 0 0-8.126238-13.757017 16.124503 16.124503 0 0 0-11.901419-5.11889c-13.629044 0-24.954588 18.555976-34.232576 55.156038h-32.249006a305.853668 305.853668 0 0 1 25.274519-64.050109c8.382182-15.484642 19.64374-29.113686 33.080826-39.991327 11.517502-8.830085 25.274519-13.629044 39.479437-13.69303 17.148281-0.639861 33.592715 7.038474 44.7263 20.731504 12.797225 16.636392 22.267171 35.832229 27.833964 56.43576l5.822737 18.939893 54.580163-61.042762c14.076947-19.323809 35.064395-31.993062 58.035414-35.064395a31.225228 31.225228 0 0 1 32.568936 17.084295c4.670987 10.685683 7.166446 22.395143 7.230432 34.168589v-0.895805z";


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

        /// <summary>表达式文本</summary>
        public string Expression
        {
            get => _expression;
            set
            {
                SetProperty(ref _expression, value);
                RaisePropertyChanged(nameof(ExpressionPreview));
                RaisePropertyChanged(nameof(InstanceInfo));
            }
        }

        #endregion

        #region 显示属性

        public string ExpressionPreview
        {
            get
            {
                if (string.IsNullOrEmpty(_expression))
                    return "未输入表达式";
                var preview = _expression.Length > 40 ? _expression[..40] + "..." : _expression;
                return preview;
            }
        }

        public string InstanceInfo
        {
            get
            {
                var sb = new StringBuilder();
                sb.AppendLine($"表达式 [{Title}]");
                if (!string.IsNullOrEmpty(_expression))
                    sb.AppendLine(ExpressionPreview);
                if (Inputs.Count > 0)
                    sb.AppendLine($"入: {string.Join(", ", Inputs.Select(i => $"{i.Title}({i.DataType})"))}");
                if (Outputs.Count > 0)
                    sb.AppendLine($"出: {string.Join(", ", Outputs.Select(o => $"{o.Title}({o.DataType})"))}");
                if (!string.IsNullOrEmpty(StatusMessage))
                    sb.Append(StatusMessage);
                return sb.ToString().TrimEnd();
            }
        }

        #endregion

        #region 命令

        private ICommand? _addInputCommand;
        private ICommand? _deleteInputCommand;
        private ICommand? _addOutputCommand;
        private ICommand? _deleteOutputCommand;

        public ICommand AddInputCommand => _addInputCommand ??= new RelayCommand<DataType?>(AddInput);
        public ICommand DeleteInputCommand => _deleteInputCommand ??= new RelayCommand<ConnectorViewModel?>(DeleteInput);
        public ICommand AddOutputCommand => _addOutputCommand ??= new RelayCommand<DataType?>(AddOutput);
        public ICommand DeleteOutputCommand => _deleteOutputCommand ??= new RelayCommand<ConnectorViewModel?>(DeleteOutput);

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

        private void AddOutput(DataType? dataType)
        {
            var dt = dataType ?? DataType.String;
            if (dt == DataType.TCPAgreement || dt == DataType.Any) return;

            var connector = new ConnectorViewModel
            {
                Title = $"输出{Outputs.Count + 1}",
                DataType = dt,
                Direction = ConnectorDirection.Output
            };
            connector.PropertyChanged += OnConnectorPropertyChanged;
            Outputs.Add(connector);

            RaisePropertyChanged(nameof(InstanceInfo));
        }

        private void DeleteOutput(ConnectorViewModel? connector)
        {
            if (connector == null || !Outputs.Contains(connector)) return;
            DisconnectConnector(connector);
            connector.PropertyChanged -= OnConnectorPropertyChanged;
            Outputs.Remove(connector);

            RaisePropertyChanged(nameof(InstanceInfo));
        }
        public ExpressionOperationModelView()
        {
            var resultOutput = new ConnectorViewModel
            {
                Title = "Result",
                DataType = DataType.Bool,
                Direction = ConnectorDirection.Output
            };
            resultOutput.Value = false;
            resultOutput.PropertyChanged += OnConnectorPropertyChanged;
            Outputs.Add(resultOutput);
        }
        private void OnConnectorPropertyChanged(object? sender, PropertyChangedEventArgs e)
        {
            if (sender is not ConnectorViewModel connector) return;

            if (e.PropertyName == nameof(ConnectorViewModel.Title) ||
                e.PropertyName == nameof(ConnectorViewModel.DataType))
            {
                // 如果当前连接器存在连线，修改类型必须断开所有连线
                if (e.PropertyName == nameof(ConnectorViewModel.DataType))
                    if (connector.IsConnected)
                    {
                        DisconnectConnector(connector);
                    }
                RaisePropertyChanged(nameof(InstanceInfo));
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
            try
            {
                if (string.IsNullOrWhiteSpace(_expression))
                {
                    StatusMessage = "表达式为空";
                    RunStatus = NodeRunStatus.Error;
                    return false;
                }

                if (Outputs.Count == 0)
                {
                    StatusMessage = "无输出连接器";
                    RunStatus = NodeRunStatus.Error;
                    return false;
                }

                // 构建变量字典：输入名称 -> 输入值
                var variables = new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase);
                foreach (var input in Inputs)
                {
                    variables[input.Title] = input.Value;
                }

                // 解析并计算表达式
                var result = EvaluateExpression(_expression, variables);

                // 设置输出值
                Outputs[0].Value = result;
                StatusMessage = $"执行成功";
                RunStatus = NodeRunStatus.Completed;
                RaisePropertyChanged(nameof(InstanceInfo));
                return true;
            }
            catch (Exception ex)
            {
                StatusMessage = $"错误: {ex.Message}";
                RunStatus = NodeRunStatus.Error;
                Outputs[0].Value = null;
                return false;
            }
        }

        #endregion

        #region 表达式引擎

        /// <summary>
        /// 表达式求值引擎
        /// 支持：算术运算、三角函数、字符串处理、逻辑运算、Linq 风格操作
        /// </summary>
        private object? EvaluateExpression(string expr, Dictionary<string, object?> variables)
        {
            expr = expr.Trim();

            // 处理多行：如果有多行语句，取最后一行作为结果
            var lines = expr.Split('\n', StringSplitOptions.RemoveEmptyEntries);
            if (lines.Length > 1)
            {
                // 多行模式：逐行执行，每行可以是赋值或表达式
                object? lastResult = null;
                foreach (var line in lines)
                {
                    var trimmed = line.Trim();
                    if (string.IsNullOrEmpty(trimmed)) continue;

                    // 处理赋值语句：var = expression
                    var assignMatch = Regex.Match(trimmed, @"^(\w+)\s*=\s*(.+)$");
                    if (assignMatch.Success)
                    {
                        var varName = assignMatch.Groups[1].Value;
                        var valueExpr = assignMatch.Groups[2].Value;
                        var value = EvaluateSingleExpression(valueExpr, variables);
                        variables[varName] = value;
                        lastResult = value;
                    }
                    else
                    {
                        lastResult = EvaluateSingleExpression(trimmed, variables);
                    }
                }
                return lastResult;
            }

            return EvaluateSingleExpression(expr, variables);
        }

        private object? EvaluateSingleExpression(string expr, Dictionary<string, object?> variables)
        {
            // 预处理：替换变量引用
            expr = ReplaceVariables(expr, variables);

            // 尝试用 DataTable.Compute 计算（纯数学表达式）
            try
            {
                var dt = new DataTable();
                var result = dt.Compute(expr, "");
                return result;
            }
            catch
            {
                // DataTable.Compute 失败，尝试函数调用解析
            }

            // 尝试解析函数调用
            return EvaluateFunctionCall(expr, variables);
        }

        /// <summary>
        /// 替换表达式中的变量引用
        /// </summary>
        private string ReplaceVariables(string expr, Dictionary<string, object?> variables)
        {
            // 先替换变量（将变量名替换为数值，确保后续数学函数能正确求值）
            foreach (var kv in variables.OrderByDescending(v => v.Key.Length))
            {
                if (string.IsNullOrEmpty(kv.Key)) continue;
                // 仅替换单词边界的变量名（避免替换函数名中的部分）
                var pattern = $@"\b{Regex.Escape(kv.Key)}\b";
                var valueStr = FormatValue(kv.Value);
                expr = Regex.Replace(expr, pattern, valueStr, RegexOptions.IgnoreCase);
            }

            // 再替换数学函数为 DataTable 兼容形式（此时变量已替换为数值）
            expr = ReplaceMathFunctions(expr);

            return expr;
        }

        /// <summary>
        /// 替换数学函数为 DataTable.Compute 支持的等价形式
        /// </summary>
        private static string ReplaceMathFunctions(string expr)
        {
            // sin(x) → 使用数值计算
            expr = Regex.Replace(expr, @"\bsin\s*\(\s*([^)]+)\s*\)", m =>
                Math.Sin(double.Parse(EvalSimple(m.Groups[1].Value))).ToString(CultureInfo.InvariantCulture),
                RegexOptions.IgnoreCase);

            expr = Regex.Replace(expr, @"\bcos\s*\(\s*([^)]+)\s*\)", m =>
                Math.Cos(double.Parse(EvalSimple(m.Groups[1].Value))).ToString(CultureInfo.InvariantCulture),
                RegexOptions.IgnoreCase);

            expr = Regex.Replace(expr, @"\btan\s*\(\s*([^)]+)\s*\)", m =>
                Math.Tan(double.Parse(EvalSimple(m.Groups[1].Value))).ToString(CultureInfo.InvariantCulture),
                RegexOptions.IgnoreCase);

            expr = Regex.Replace(expr, @"\bsqrt\s*\(\s*([^)]+)\s*\)", m =>
                Math.Sqrt(double.Parse(EvalSimple(m.Groups[1].Value))).ToString(CultureInfo.InvariantCulture),
                RegexOptions.IgnoreCase);

            expr = Regex.Replace(expr, @"\babs\s*\(\s*([^)]+)\s*\)", m =>
                Math.Abs(double.Parse(EvalSimple(m.Groups[1].Value))).ToString(CultureInfo.InvariantCulture),
                RegexOptions.IgnoreCase);

            expr = Regex.Replace(expr, @"\bpow\s*\(\s*([^,]+),\s*([^)]+)\s*\)", m =>
                Math.Pow(double.Parse(EvalSimple(m.Groups[1].Value)), double.Parse(EvalSimple(m.Groups[2].Value))).ToString(CultureInfo.InvariantCulture),
                RegexOptions.IgnoreCase);

            expr = Regex.Replace(expr, @"\blog\s*\(\s*([^,]+),\s*([^)]+)\s*\)", m =>
                Math.Log(double.Parse(EvalSimple(m.Groups[2].Value)), double.Parse(EvalSimple(m.Groups[1].Value))).ToString(CultureInfo.InvariantCulture),
                RegexOptions.IgnoreCase);

            expr = Regex.Replace(expr, @"\blog10\s*\(\s*([^)]+)\s*\)", m =>
                Math.Log10(double.Parse(EvalSimple(m.Groups[1].Value))).ToString(CultureInfo.InvariantCulture),
                RegexOptions.IgnoreCase);

            expr = Regex.Replace(expr, @"\bln\s*\(\s*([^)]+)\s*\)", m =>
                Math.Log(double.Parse(EvalSimple(m.Groups[1].Value))).ToString(CultureInfo.InvariantCulture),
                RegexOptions.IgnoreCase);

            expr = Regex.Replace(expr, @"\bceil\s*\(\s*([^)]+)\s*\)", m =>
                Math.Ceiling(double.Parse(EvalSimple(m.Groups[1].Value))).ToString(CultureInfo.InvariantCulture),
                RegexOptions.IgnoreCase);

            expr = Regex.Replace(expr, @"\bfloor\s*\(\s*([^)]+)\s*\)", m =>
                Math.Floor(double.Parse(EvalSimple(m.Groups[1].Value))).ToString(CultureInfo.InvariantCulture),
                RegexOptions.IgnoreCase);

            expr = Regex.Replace(expr, @"\bround\s*\(\s*([^,)]+)(?:,\s*([^)]+))?\s*\)", m =>
            {
                var val = double.Parse(EvalSimple(m.Groups[1].Value));
                var digits = m.Groups[2].Success ? int.Parse(EvalSimple(m.Groups[2].Value)) : 0;
                return Math.Round(val, digits).ToString(CultureInfo.InvariantCulture);
            }, RegexOptions.IgnoreCase);

            expr = Regex.Replace(expr, @"\bPI\b", Math.PI.ToString(CultureInfo.InvariantCulture), RegexOptions.IgnoreCase);
            expr = Regex.Replace(expr, @"\bE\b", Math.E.ToString(CultureInfo.InvariantCulture));

            return expr;
        }

        /// <summary>
        /// 简单数值计算（用于函数参数求值）
        /// </summary>
        private static string EvalSimple(string expr)
        {
            try
            {
                var dt = new DataTable();
                var result = dt.Compute(expr.Trim(), "");
                return result?.ToString() ?? "0";
            }
            catch
            {
                return expr.Trim();
            }
        }

        /// <summary>
        /// 格式化值为字符串
        /// </summary>
        private static string FormatValue(object? value)
        {
            return value switch
            {
                null => "null",
                string s => $"\"{s}\"",
                bool b => b ? "true" : "false",
                double d => d.ToString(CultureInfo.InvariantCulture),
                float f => f.ToString(CultureInfo.InvariantCulture),
                int i => i.ToString(),
                _ => value.ToString() ?? "null"
            };
        }

        /// <summary>
        /// 解析函数调用：func(arg1, arg2, ...)
        /// </summary>
        private object? EvaluateFunctionCall(string expr, Dictionary<string, object?> variables)
        {
            // 字符串函数
            var strFuncMatch = Regex.Match(expr, @"^(concat|substring|replace|trim|toupper|tolower|length|contains|startswith|endswith|indexof|split|join|format)\s*\((.+)\)$", RegexOptions.IgnoreCase);
            if (strFuncMatch.Success)
            {
                return EvaluateStringFunction(strFuncMatch.Groups[1].Value, strFuncMatch.Groups[2].Value, variables);
            }

            // Linq 风格函数
            var linqMatch = Regex.Match(expr, @"^(sum|avg|min|max|count|first|last|where|select|any|all|orderby|distinct)\s*\((.+)\)$", RegexOptions.IgnoreCase);
            if (linqMatch.Success)
            {
                return EvaluateLinqFunction(linqMatch.Groups[1].Value, linqMatch.Groups[2].Value, variables);
            }

            // 逻辑函数
            var logicMatch = Regex.Match(expr, @"^(if|iif|and|or|not)\s*\((.+)\)$", RegexOptions.IgnoreCase);
            if (logicMatch.Success)
            {
                return EvaluateLogicFunction(logicMatch.Groups[1].Value, logicMatch.Groups[2].Value, variables);
            }

            return expr;
        }

        #region 字符串函数

        private object? EvaluateStringFunction(string func, string args, Dictionary<string, object?> variables)
        {
            var parts = SplitArgs(args);
            var resolved = parts.Select(p => ResolveArg(p.Trim(), variables)).ToArray();

            return func.ToLower() switch
            {
                "concat" => string.Concat(resolved.Select(r => r?.ToString() ?? "")),
                "substring" => resolved.Length >= 2
                    ? resolved[0]?.ToString()?.Substring(
                        Math.Max(0, Convert.ToInt32(resolved[1])),
                        resolved.Length >= 3 ? Math.Min(Convert.ToInt32(resolved[2]), (resolved[0]?.ToString()?.Length ?? 0) - Convert.ToInt32(resolved[1])) : (resolved[0]?.ToString()?.Length ?? 0) - Convert.ToInt32(resolved[1]))
                    : resolved[0]?.ToString(),
                "replace" => resolved.Length >= 3
                    ? resolved[0]?.ToString()?.Replace(resolved[1]?.ToString() ?? "", resolved[2]?.ToString() ?? "")
                    : resolved[0]?.ToString(),
                "trim" => resolved[0]?.ToString()?.Trim(),
                "toupper" => resolved[0]?.ToString()?.ToUpper(),
                "tolower" => resolved[0]?.ToString()?.ToLower(),
                "length" => resolved[0]?.ToString()?.Length,
                "contains" => resolved.Length >= 2
                    ? resolved[0]?.ToString()?.Contains(resolved[1]?.ToString() ?? "")
                    : false,
                "startswith" => resolved.Length >= 2
                    ? resolved[0]?.ToString()?.StartsWith(resolved[1]?.ToString() ?? "")
                    : false,
                "endswith" => resolved.Length >= 2
                    ? resolved[0]?.ToString()?.EndsWith(resolved[1]?.ToString() ?? "")
                    : false,
                "indexof" => resolved.Length >= 2
                    ? resolved[0]?.ToString()?.IndexOf(resolved[1]?.ToString() ?? "")
                    : -1,
                "split" => resolved.Length >= 2
                    ? resolved[0]?.ToString()?.Split(new[] { resolved[1]?.ToString() ?? "" }, StringSplitOptions.None)
                    : new[] { resolved[0]?.ToString() ?? "" },
                "join" => resolved.Length >= 2 && resolved[0] is string[] arr
                    ? string.Join(resolved[1]?.ToString() ?? ",", arr)
                    : resolved[0]?.ToString(),
                "format" => resolved.Length >= 2
                    ? string.Format(resolved[0]?.ToString() ?? "", resolved.Skip(1).ToArray())
                    : resolved[0]?.ToString(),
                _ => null
            };
        }

        #endregion

        #region Linq 函数

        private object? EvaluateLinqFunction(string func, string args, Dictionary<string, object?> variables)
        {
            var parts = SplitArgs(args);
            var resolved = parts.Select(p => ResolveArg(p.Trim(), variables)).ToArray();

            // 获取集合
            var collection = resolved[0];
            IEnumerable<object?> enumerable;

            if (collection is IEnumerable<object?> en)
                enumerable = en;
            else if (collection is System.Collections.IEnumerable ie)
                enumerable = ie.Cast<object?>();
            else
                return null;

            var list = enumerable.ToList();

            return func.ToLower() switch
            {
                "sum" => list.Where(x => x != null).Select(x => Convert.ToDouble(x)).Sum(),
                "avg" => list.Where(x => x != null).Select(x => Convert.ToDouble(x)).Average(),
                "min" => list.Where(x => x != null).Select(x => Convert.ToDouble(x)).Min(),
                "max" => list.Where(x => x != null).Select(x => Convert.ToDouble(x)).Max(),
                "count" => list.Count,
                "first" => list.FirstOrDefault(),
                "last" => list.LastOrDefault(),
                "distinct" => list.Distinct().ToList(),
                "any" => resolved.Length >= 2
                    ? list.Any(x => CheckPredicate(x, resolved[1]?.ToString() ?? ""))
                    : list.Any(),
                "all" => resolved.Length >= 2
                    ? list.All(x => CheckPredicate(x, resolved[1]?.ToString() ?? ""))
                    : true,
                "where" => resolved.Length >= 2
                    ? list.Where(x => CheckPredicate(x, resolved[1]?.ToString() ?? "")).ToList()
                    : list,
                "select" => resolved.Length >= 2
                    ? list.Select(x => GetProperty(x, resolved[1]?.ToString() ?? "")).ToList()
                    : list,
                "orderby" => resolved.Length >= 2
                    ? list.OrderBy(x => GetProperty(x, resolved[1]?.ToString() ?? "")).ToList()
                    : list,
                _ => list
            };
        }

        private static bool CheckPredicate(object? item, string condition)
        {
            if (item == null) return false;
            // 简单比较：属性 运算符 值
            var match = Regex.Match(condition, @"(\w+)\s*(==|!=|>=|<=|>|<)\s*(.+)");
            if (match.Success)
            {
                var prop = GetProperty(item, match.Groups[1].Value);
                var op = match.Groups[2].Value;
                var val = match.Groups[3].Value.Trim().Trim('"', '\'');
                var propStr = prop?.ToString() ?? "";

                int cmp;
                if (double.TryParse(propStr, out var pNum) && double.TryParse(val, out var vNum))
                    cmp = pNum.CompareTo(vNum);
                else
                    cmp = string.Compare(propStr, val, StringComparison.OrdinalIgnoreCase);

                return op switch
                {
                    "==" => cmp == 0,
                    "!=" => cmp != 0,
                    ">=" => cmp >= 0,
                    "<=" => cmp <= 0,
                    ">" => cmp > 0,
                    "<" => cmp < 0,
                    _ => false
                };
            }

            // 简单布尔检查
            if (condition.Trim().Equals("true", StringComparison.OrdinalIgnoreCase))
                return item is bool b && b;
            if (condition.Trim().Equals("false", StringComparison.OrdinalIgnoreCase))
                return item is bool b && !b;

            return item.ToString()?.Contains(condition.Trim()) ?? false;
        }

        private static object? GetProperty(object? item, string propName)
        {
            if (item == null) return null;
            var prop = item.GetType().GetProperty(propName,
                System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.IgnoreCase);
            return prop?.GetValue(item);
        }

        #endregion

        #region 逻辑函数

        private object? EvaluateLogicFunction(string func, string args, Dictionary<string, object?> variables)
        {
            var parts = SplitArgs(args);
            var resolved = parts.Select(p => ResolveArg(p.Trim(), variables)).ToArray();

            return func.ToLower() switch
            {
                "if" or "iif" => resolved.Length >= 3
                    ? (IsTruthy(resolved[0]) ? resolved[1] : resolved[2])
                    : null,
                "and" => resolved.All(IsTruthy),
                "or" => resolved.Any(IsTruthy),
                "not" => resolved.Length >= 1 ? !IsTruthy(resolved[0]) : null,
                _ => null
            };
        }

        private static bool IsTruthy(object? value)
        {
            return value switch
            {
                null => false,
                bool b => b,
                string s => !string.IsNullOrEmpty(s) && !s.Equals("false", StringComparison.OrdinalIgnoreCase),
                int i => i != 0,
                double d => d != 0,
                _ => true
            };
        }

        #endregion

        /// <summary>
        /// 解析参数值
        /// </summary>
        private static object? ResolveArg(string arg, Dictionary<string, object?> variables)
        {
            if (string.IsNullOrEmpty(arg)) return null;

            // 字符串字面量
            if (arg.StartsWith('"') && arg.EndsWith('"'))
                return arg[1..^1];
            if (arg.StartsWith('\'') && arg.EndsWith('\''))
                return arg[1..^1];

            // 布尔字面量
            if (arg.Equals("true", StringComparison.OrdinalIgnoreCase)) return true;
            if (arg.Equals("false", StringComparison.OrdinalIgnoreCase)) return false;
            if (arg.Equals("null", StringComparison.OrdinalIgnoreCase)) return null;

            // 数值字面量
            if (double.TryParse(arg, NumberStyles.Float, CultureInfo.InvariantCulture, out var d))
                return d;

            // 变量引用
            if (variables.TryGetValue(arg, out var val))
                return val;

            // 尝试作为表达式再次求值
            try
            {
                var dt = new DataTable();
                return dt.Compute(arg, "");
            }
            catch
            {
                return arg;
            }
        }

        /// <summary>
        /// 分割函数参数（支持嵌套括号和引号）
        /// </summary>
        private static string[] SplitArgs(string args)
        {
            var result = new List<string>();
            var current = new StringBuilder();
            int depth = 0;
            bool inString = false;
            char stringChar = '"';

            foreach (var c in args)
            {
                if (inString)
                {
                    current.Append(c);
                    if (c == stringChar) inString = false;
                    continue;
                }

                switch (c)
                {
                    case '"' or '\'':
                        inString = true;
                        stringChar = c;
                        current.Append(c);
                        break;
                    case '(':
                        depth++;
                        current.Append(c);
                        break;
                    case ')':
                        depth--;
                        current.Append(c);
                        break;
                    case ',' when depth == 0:
                        result.Add(current.ToString());
                        current.Clear();
                        break;
                    default:
                        current.Append(c);
                        break;
                }
            }

            if (current.Length > 0)
                result.Add(current.ToString());

            return result.ToArray();
        }

        #endregion

        #region ISerializableOperation

        private const int SerializeVersion = 1;

        public void Serialize(BinaryWriter writer)
        {
            writer.Write(SerializeVersion);
            writer.Write(Title);
            writer.Write(_expression ?? "");

            // 输入
            writer.Write(Inputs.Count);
            foreach (var input in Inputs)
            {
                writer.Write(input.Title);
                writer.Write((int)input.DataType);
            }

            // 输出
            writer.Write(Outputs.Count);
            foreach (var output in Outputs)
            {
                writer.Write(output.Title);
                writer.Write((int)output.DataType);
            }
        }

        public void Deserialize(BinaryReader reader)
        {
            var version = reader.ReadInt32();
            Title = reader.ReadString();
            _expression = reader.ReadString();

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

            Outputs.Clear();
            var outputCount = reader.ReadInt32();
            for (int i = 0; i < outputCount; i++)
            {
                var connector = new ConnectorViewModel
                {
                    Title = reader.ReadString(),
                    DataType = (DataType)reader.ReadInt32(),
                    Direction = ConnectorDirection.Output
                };
                connector.PropertyChanged += OnConnectorPropertyChanged;
                Outputs.Add(connector);
            }

            RaisePropertyChanged(nameof(Expression));
            RaisePropertyChanged(nameof(ExpressionPreview));
            RaisePropertyChanged(nameof(InstanceInfo));
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
                $"[ExpressionOperationModelView] 已断开 '{connector.Title}' 上的 {toRemove.Count} 条连线");
        }

        #endregion

    }
}