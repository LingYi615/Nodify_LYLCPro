using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Windows;
using System.Windows.Input;
using LYCorePro.Common.Helper;
using LYCorePro.Core;
using Nodify;
using PluginBase.Interfaces;
using PluginBase.Models;

namespace PythonScriptPlugin
{
    /// <summary>
    /// Python 脚本操作（节点实例）
    /// - 初始无输入/输出，支持动态添加
    /// - 输入/输出数据类型可自定义修改
    /// - 支持代码编辑和校验
    /// - 通过 Python 子进程执行脚本（支持完整 Python 生态）
    /// </summary>
    public class PythonScriptOperationModelView : NotifyPropertyBase, IExecutableOperation, ISerializableOperation, IConnectionAware
    {
        #region 静态资源

        public static DataType[] AvailableDataTypes { get; } = Enum
            .GetValues(typeof(DataType))
            .Cast<DataType>()
            .Where(dt => dt != DataType.TCPAgreement && dt != DataType.Any)
            .ToArray();

        private static readonly JsonSerializerOptions _jsonOptions = new()
        {
            WriteIndented = false,
            PropertyNamingPolicy = null,
            NumberHandling = System.Text.Json.Serialization.JsonNumberHandling.AllowNamedFloatingPointLiterals
        };

        private static readonly Encoding Utf8NoBom = new UTF8Encoding(false);

        #endregion

        #region 字段

        private string _title = "Python脚本";
        private string? _statusMessage;
        private NodeRunStatus _runStatus;

        // IConnectionAware 实现
        private ObservableCollection<ConnectionViewModel>? _connections;
        private string _pythonCode = "";
        private string? _validationResult;
        private bool _isValid = true;
        private string _pythonPath = "python";
        private string _icon = "M551.384615 0H472.615385a157.538462 157.538462 0 0 0-157.538462 157.538462v78.76923h236.307692v78.769231H157.538462a157.538462 157.538462 0 0 0-157.538462 157.538462v78.76923a157.538462 157.538462 0 0 0 157.538462 157.538462h78.76923v-59.076923A120.516923 120.516923 0 0 1 354.461538 527.753846h315.076924a40.172308 40.172308 0 0 0 39.384615-40.96V157.538462a157.538462 157.538462 0 0 0-157.538462-157.538462zM433.230769 157.538462a39.384615 39.384615 0 1 1 39.384616-39.384616 39.384615 39.384615 0 0 1-39.384616 39.384616z M866.461538 315.076923h-78.76923v171.716923a120.516923 120.516923 0 0 1-118.153846 122.092308h-315.076924a40.172308 40.172308 0 0 0-39.384615 40.96V866.461538a157.538462 157.538462 0 0 0 157.538462 157.538462h78.76923a157.538462 157.538462 0 0 0 157.538462-157.538462v-78.76923H472.615385V708.923077h304.836923a325.316923 325.316923 0 0 1 85.070769-130.756923 242.609231 242.609231 0 0 1 157.538461-65.378462V472.615385A157.538462 157.538462 0 0 0 866.461538 315.076923z m-275.692307 551.384615a39.384615 39.384615 0 1 1-39.384616 39.384616 39.384615 39.384615 0 0 1 39.384616-39.384616z M889.304615 607.310769A319.803077 319.803077 0 0 0 795.569231 787.692308a236.307692 236.307692 0 0 0 17.329231 172.504615 140.209231 140.209231 0 0 0 128.393846 62.227692 193.772308 193.772308 0 0 0 127.606154-46.473846 267.027692 267.027692 0 0 0 78.76923-119.729231H1063.384615a185.895385 185.895385 0 0 1-32.295384 51.987693 100.036923 100.036923 0 0 1-78.769231 32.295384 70.892308 70.892308 0 0 1-66.166154-37.80923 169.353846 169.353846 0 0 1-2.363077-115.003077 251.273846 251.273846 0 0 1 50.412308-117.366154A106.338462 106.338462 0 0 1 1016.910769 630.153846a68.529231 68.529231 0 0 1 64.590769 29.144616 107.913846 107.913846 0 0 1 11.815385 48.836923h86.646154a148.873846 148.873846 0 0 0-11.815385-84.283077A136.270769 136.270769 0 0 0 1029.513846 551.384615a203.224615 203.224615 0 0 0-140.209231 55.926154z";

        #endregion

        #region 节点属性

        public string Title
        {
            get => _title;
            set { SetProperty(ref _title, value); RaisePropertyChanged(nameof(InstanceInfo)); }
        }

        public string Icon
        {
            get => _icon;
            set { SetProperty(ref _icon, value); }
        }

        public ObservableCollection<ConnectorViewModel> Inputs { get; } = new();
        public ObservableCollection<ConnectorViewModel> Outputs { get; } = new();

        /// <summary>Python 脚本代码</summary>
        public string PythonCode
        {
            get => _pythonCode;
            set
            {
                SetProperty(ref _pythonCode, value);
                RaisePropertyChanged(nameof(CodePreview));
                RaisePropertyChanged(nameof(InstanceInfo));
                ValidateCode();
            }
        }

        /// <summary>Python 解释器路径（默认 "python"，可改为完整路径如 "C:\Python39\python.exe"）</summary>
        public string PythonPath
        {
            get => _pythonPath;
            set { SetProperty(ref _pythonPath, value); }
        }

        /// <summary>校验结果</summary>
        public string? ValidationResult
        {
            get => _validationResult;
            set { SetProperty(ref _validationResult, value); RaisePropertyChanged(nameof(IsValidationError)); }
        }

        /// <summary>是否校验通过</summary>
        public bool IsValid
        {
            get => _isValid;
            set { SetProperty(ref _isValid, value); RaisePropertyChanged(nameof(InstanceInfo)); }
        }

        /// <summary>是否有校验错误（用于 UI 显示错误颜色）</summary>
        public bool IsValidationError => !string.IsNullOrEmpty(_validationResult);

        #endregion

        #region 显示属性

        public string CodePreview
        {
            get
            {
                if (string.IsNullOrEmpty(_pythonCode))
                    return "未输入代码";
                var firstLine = _pythonCode.Split('\n')[0].Trim();
                return firstLine.Length > 50 ? firstLine[..50] + "..." : firstLine;
            }
        }

        public string InstanceInfo
        {
            get
            {
                var sb = new StringBuilder();
                sb.AppendLine($"Python [{Title}]");
                if (!string.IsNullOrEmpty(_pythonCode))
                    sb.AppendLine(CodePreview);
                if (Inputs.Count > 0)
                    sb.AppendLine($"入: {string.Join(", ", Inputs.Select(i => $"{i.Title}({i.DataType})"))}");
                if (Outputs.Count > 0)
                    sb.AppendLine($"出: {string.Join(", ", Outputs.Select(o => $"{o.Title}({o.DataType})"))}");
                if (!string.IsNullOrEmpty(_validationResult))
                    sb.AppendLine($"校验: {_validationResult}");
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
        private ICommand? _validateCommand;

        public ICommand AddInputCommand => _addInputCommand ??= new RelayCommand<DataType?>(AddInput);
        public ICommand DeleteInputCommand => _deleteInputCommand ??= new RelayCommand<ConnectorViewModel?>(DeleteInput);
        public ICommand AddOutputCommand => _addOutputCommand ??= new RelayCommand<DataType?>(AddOutput);
        public ICommand DeleteOutputCommand => _deleteOutputCommand ??= new RelayCommand<ConnectorViewModel?>(DeleteOutput);
        public ICommand ValidateCommand => _validateCommand ??= new RelayCommand(ValidateCode);

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

        public PythonScriptOperationModelView()
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

        #region 代码校验 —— 使用 python -m py_compile

        public void ValidateCode()
        {
            if (string.IsNullOrWhiteSpace(_pythonCode))
            {
                ValidationResult = null;
                IsValid = true;
                return;
            }

            try
            {
                var tempFile = Path.Combine(Path.GetTempPath(), $"lcpro_check_{Guid.NewGuid():N}.py");
                File.WriteAllText(tempFile, _pythonCode, Utf8NoBom);

                try
                {
                    var psi = new ProcessStartInfo
                    {
                        FileName = _pythonPath,
                        Arguments = $"-m py_compile \"{tempFile}\"",
                        RedirectStandardInput = true,
                        RedirectStandardOutput = true,
                        RedirectStandardError = true,
                        UseShellExecute = false,
                        CreateNoWindow = true,
                        StandardInputEncoding = Utf8NoBom,
                        StandardOutputEncoding = Utf8NoBom,
                        StandardErrorEncoding = Utf8NoBom
                    };
                    psi.EnvironmentVariables["PYTHONIOENCODING"] = "utf-8";

                    using var process = new Process { StartInfo = psi };
                    process.Start();
                    var error = process.StandardError.ReadToEnd();
                    process.WaitForExit(5000);

                    if (process.ExitCode == 0)
                    {
                        ValidationResult = null;
                        IsValid = true;
                    }
                    else
                    {
                        ValidationResult = error.Trim();
                        IsValid = false;
                    }
                }
                finally
                {
                    try { File.Delete(tempFile); } catch { }
                }
            }
            catch (Exception ex)
            {
                ValidationResult = $"校验异常: {ex.Message}";
                IsValid = false;
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
            set { SetProperty(ref _runStatus, value); }
        }

        public bool Execute()
        {
            try
            {
                if (string.IsNullOrWhiteSpace(_pythonCode))
                {
                    StatusMessage = "Python 代码为空";
                    RunStatus = NodeRunStatus.Error;
                    return false;
                }

                if (!_isValid)
                {
                    StatusMessage = $"代码校验未通过: {_validationResult}";
                    RunStatus = NodeRunStatus.Error;
                    return false;
                }

                // 构建输入变量字典
                var inputDict = new Dictionary<string, object?>();
                foreach (var input in Inputs)
                {
                    inputDict[input.Title] = input.Value;
                }

                // 通过 Python 子进程执行脚本
                var result = ExecutePythonViaProcess(_pythonCode, inputDict);

                // 设置输出值
                if (Outputs.Count > 0 && result != null)
                {
                    if (result is Dictionary<string, object?> resultDict)
                    {
                        foreach (var output in Outputs)
                        {
                            if (resultDict.TryGetValue(output.Title, out var val))
                                output.Value = val;
                        }
                    }
                    else
                    {
                        Outputs[0].Value = result;
                    }
                }

                StatusMessage = "执行成功";
                RunStatus = NodeRunStatus.Completed;
                return true;
            }
            catch (Exception ex)
            {
                StatusMessage = $"执行错误: {ex.Message}";
                RunStatus = NodeRunStatus.Error;
                return false;
            }
        }

        #endregion

        #region Python 子进程执行引擎

        /// <summary>
        /// 通过子进程调用真实 Python 解释器执行脚本
        /// - 输入通过 stdin 传入 JSON
        /// - 输出通过 stdout 返回 JSON
        /// - 自动注入 math, random, datetime, re, json, collections, itertools, functools 等标准库
        /// </summary>
        private object? ExecutePythonViaProcess(string code, Dictionary<string, object?> inputs)
        {
            // 1. 序列化输入为 JSON
            var inputJson = JsonSerializer.Serialize(inputs, _jsonOptions);

            // 2. 构建完整的 Python 脚本（包装用户代码）
            var fullScript = BuildPythonScript(code);

            // 3. 写入临时文件
            var tempFile = Path.Combine(Path.GetTempPath(), $"lcpro_py_{Guid.NewGuid():N}.py");
            File.WriteAllText(tempFile, fullScript, Utf8NoBom);

            try
            {
                // 4. 启动 Python 进程
                var psi = new ProcessStartInfo
                {
                    FileName = _pythonPath,
                    Arguments = $"\"{tempFile}\"",
                    RedirectStandardInput = true,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    UseShellExecute = false,
                    CreateNoWindow = true,
                    StandardInputEncoding = Utf8NoBom,
                    StandardOutputEncoding = Utf8NoBom,
                    StandardErrorEncoding = Utf8NoBom
                };
                psi.EnvironmentVariables["PYTHONIOENCODING"] = "utf-8";

                using var process = new Process { StartInfo = psi };
                process.Start();

                // 5. 通过 stdin 传入输入数据
                process.StandardInput.Write(inputJson);
                process.StandardInput.Close();

                // 6. 读取输出
                var output = process.StandardOutput.ReadToEnd();
                var error = process.StandardError.ReadToEnd();

                if (!process.WaitForExit(10000))
                {
                    process.Kill();
                    throw new Exception("Python 脚本执行超时（10秒）");
                }

                if (process.ExitCode != 0)
                {
                    throw new Exception(error.Trim());
                }

                if (string.IsNullOrWhiteSpace(output))
                {
                    return null;
                }

                // 7. 反序列化输出
                return JsonSerializer.Deserialize<Dictionary<string, object?>>(output, _jsonOptions);
            }
            finally
            {
                // 8. 清理临时文件
                try { File.Delete(tempFile); } catch { }
            }
        }

        /// <summary>
        /// 构建包含用户代码的完整 Python 脚本
        /// 自动注入标准库，通过 stdin 读取输入，通过 stdout 输出结果
        /// </summary>
        private static string BuildPythonScript(string userCode)
        {
            return $@"# -*- coding: utf-8 -*-
import sys
import io
sys.stdout = io.TextIOWrapper(sys.stdout.buffer, encoding='utf-8', errors='replace')
sys.stderr = io.TextIOWrapper(sys.stderr.buffer, encoding='utf-8', errors='replace')

import json
import math
import random
import datetime
import re
import collections
import itertools
import functools

# ========== 读取输入数据 ==========
input_data = sys.stdin.buffer.read().decode(""utf-8-sig"")
if not input_data:
    print(json.dumps({{}}))
    sys.exit(0)

inputs = json.loads(input_data)

# 将输入变量注入全局命名空间，自动解析JSON字符串
for k, v in inputs.items():
    if isinstance(v, str):
        s = v.strip()
        if (s.startswith('{{') and s.endswith('}}')) or (s.startswith('[') and s.endswith(']')):
            try:
                v = json.loads(s)
            except Exception:
                pass
    globals()[k] = v

# ========== 代码开始 ==========
{userCode}
# ========== 代码结束 ==========

# ========== 收集输出变量 ==========
output = {{}}
_builtin_keys = {{'__builtins__', 'input_data', 'inputs', 'k', 'v', 'output',
    'json', 'sys', 'math', 'random', 'datetime', 're', 'collections',
    'itertools', 'functools'}}

if 'result' in dir():
    output['result'] = globals().get('result')

for key in dir():
    if not key.startswith('_') and key not in _builtin_keys:
        val = globals().get(key)
        if val is not None and not callable(val) and not isinstance(val, type):
            output[key] = val

print(json.dumps(output, default=str, ensure_ascii=False))
";
        }

        #endregion

        #region ISerializableOperation

        private const int SerializeVersion = 1;

        public void Serialize(BinaryWriter writer)
        {
            writer.Write(SerializeVersion);
            writer.Write(Title);
            writer.Write(_pythonCode ?? "");
            writer.Write(_pythonPath ?? "python");

            writer.Write(Inputs.Count);
            foreach (var input in Inputs)
            {
                writer.Write(input.Title);
                writer.Write((int)input.DataType);
            }

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
            _pythonCode = reader.ReadString();
            _pythonPath = reader.ReadString();

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

            ValidateCode();
            RaisePropertyChanged(nameof(PythonCode));
            RaisePropertyChanged(nameof(CodePreview));
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
                $"[PythonScriptOperationModelView] 已断开 '{connector.Title}' 上的 {toRemove.Count} 条连线");
        }

        #endregion

    }
}