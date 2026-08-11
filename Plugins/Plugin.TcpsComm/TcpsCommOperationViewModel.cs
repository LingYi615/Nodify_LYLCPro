using System;
using System.Collections.Concurrent;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using LYCorePro.Communacation;
using LYCorePro.Common.Enum;
using LYCorePro.Core;
using Nodify;
using PluginBase.Interfaces;
using PluginBase.Models;
using System.IO;
using System.Windows.Threading;
using System.Windows;
using LYCorePro.Common.Helper;
using System.Diagnostics;

namespace TcpsCommPlugin
{
    /// <summary>
    /// TCP 通讯插件操作（节点实例）
    /// 无输入连接器，一个输出连接器（输出 ICommunication 通讯实例）
    /// 底层使用 LYCorePro 的 CommunicationFactory / CommunicationBase / TcpClientCommunication
    /// 实现 IExecutableOperation 接口，点击执行按钮时自动调用 Execute()
    /// </summary>
    public class TcpsCommOperationViewModel : NotifyPropertyBase, IExecutableOperation, ISerializableOperation
    {
        #region 字段

        private string _title = "TCP 通讯";
        private eCommunicationType _communicationType = eCommunicationType.TCPClient;
        private string _remoteIP = "127.0.0.1";
        private int _remotePort = 9000;
        private int _localPort = 8000;
        private int _timeout = 5000;
        private int _retryCount = 3;
        private bool _isSendByHex;
        private bool _isReceivedByHex;
        private ICommunication? _communicationInstance;
        private string? _statusMessage;
        private PluginBase.Interfaces.NodeRunStatus _runStatus;

        private bool _isConnected;

        private Timer? _connectionCheckTimer;
        private int _checkIntervalMs = 3000; // 默认每 3 秒检测一次

        private string _icon = "M463.36 181.0432 A268.4416 268.4416 0 0 1 843.008 560.64 L738.048 665.6 L665.6 593.2032 L770.56 488.2432 A166.0416 166.0416 0 0 0 535.7568 253.44 L430.848 358.4 L358.4 286.0032 L463.36 181.0432 Z M488.2432 770.56 L593.2032 665.6 L665.6 737.9968 L560.64 842.9568 A268.3904 268.3904 0 1 1 181.0432 463.36 L286.0032 358.4 L358.4 430.7968 L253.44 535.7568 A166.0416 166.0416 0 1 0 488.2432 770.56 Z M394.5984 701.7984 L701.7984 394.5984 L629.4016 322.2016 L322.2016 629.4016 L394.5984 701.7984 Z";

        #endregion

        #region 连接分组锁管理器（静态）

        /// <summary>
        /// 按连接参数分组的锁（相同 IP:Port 共享同一锁，不同参数并行执行）
        /// 键：(CommunicationType, RemoteIP, RemotePort)，值：SemaphoreSlim
        /// </summary>
        private static readonly ConcurrentDictionary<string, SemaphoreSlim>
            _connectionLocks = new();

        /// <summary>生成分组锁的 Key</summary>
        private string GetConnectionLockKey()
        {
            return $"{CommunicationType}_{RemoteIP}_{RemotePort}";
        }

        /// <summary>获取或创建对应连接参数的锁</summary>
        private static SemaphoreSlim GetOrCreateLock(string key)
        {
            return _connectionLocks.GetOrAdd(key, _ => new SemaphoreSlim(1, 1));
        }

        #endregion

        #region 静态资源

        /// <summary>所有通讯类型列表（供参数面板 ComboBox 绑定）</summary>
        public static eCommunicationType[] CommunicationTypes { get; } = Enum
            .GetValues(typeof(eCommunicationType))
            .Cast<eCommunicationType>()
            .Where(ct => ct != eCommunicationType.Serial && ct != eCommunicationType.ModbusRTU && ct != eCommunicationType.ModbusASCII)
            .ToArray();

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
        /// <summary>输入连接器（此插件无输入）</summary>
        public ObservableCollection<ConnectorViewModel> Inputs { get; } = new();

        /// <summary>输出连接器（输出 ICommunication 通讯实例）</summary>
        public ObservableCollection<ConnectorViewModel> Outputs { get; } = new()
        {
            new ConnectorViewModel
            {
                Title = "Instance",
                DataType = DataType.TCPAgreement,
                Direction = ConnectorDirection.Output
            }
        };

        #endregion

        #region 通讯参数属性

        /// <summary>选中的预置协议（ComboBox 绑定，选中时自动填充参数）</summary>
        public ProtocolConfig? SelectedPreset
        {
            get => _selectedPreset;
            set
            {
                if (_selectedPreset == value) return;
                SetProperty(ref _selectedPreset, value);
                if (value != null)
                {
                    CommunicationType = value.CommunicationType;
                    RemoteIP = value.Host;
                    RemotePort = value.Port;
                    Timeout = value.Timeout;
                }
            }
        }
        private ProtocolConfig? _selectedPreset;

        /// <summary>通讯类型</summary>
        public eCommunicationType CommunicationType
        {
            get => _communicationType;
            set
            {
                if (_communicationType != value)
                {
                    SetProperty(ref  _communicationType, value);
                  
                    RaisePropertyChanged(nameof(CommunicationTypeDisplay));
                    RaisePropertyChanged(nameof(IsNetworkType));
                    RaisePropertyChanged(nameof(IsNotHttpServer));
                    ApplyDefaultPorts();
                }
            }
        }

        /// <summary>通讯类型显示名称</summary>
        public string CommunicationTypeDisplay => CommunicationType.GetDisplayName();

        /// <summary>是否为网络类型（非串口）</summary>
        public bool IsNetworkType => CommunicationType.IsNetwork();

        /// <summary>是否需要远程配置（HTTP 服务器不需要）</summary>
        public bool IsNotHttpServer => CommunicationType != eCommunicationType.HTTPServer; 

        /// <summary>远程 IP 地址</summary>
        public string RemoteIP
        {
            get => _remoteIP;
            set { SetProperty(ref _remoteIP, value); }
        }

        /// <summary>远程端口</summary>
        public int RemotePort
        {
            get => _remotePort;
            set { SetProperty(ref _remotePort, value); }
        }

        /// <summary>本地端口（TCP Server / UDP 使用）</summary>
        public int LocalPort
        {
            get => _localPort;
            set { SetProperty(ref _localPort, value); }
        }

        /// <summary>超时时间（毫秒）</summary>
        public int Timeout
        {
            get => _timeout;
            set { SetProperty(ref _timeout, value); }
        }

        /// <summary>重试次数</summary>
        public int RetryCount
        {
            get => _retryCount;
            set { SetProperty(ref _retryCount, value); }
        }

        /// <summary>是否以十六进制发送</summary>
        public bool IsSendByHex
        {
            get => _isSendByHex;
            set { SetProperty(ref _isReceivedByHex, value); }
        }

        /// <summary>是否以十六进制接收</summary>
        public bool IsReceivedByHex
        {
            get => _isReceivedByHex;
            set { SetProperty(ref _isReceivedByHex, value); }
        }

        /// <summary>
        /// 通讯实例（输出连接器）
        /// 设置此属性时会自动同步到输出连接器的 Value
        /// </summary>
        public ICommunication? CommunicationInstance
        {
            get => _communicationInstance;
            set
            {
                if (_communicationInstance != value)
                {
                    SetProperty(ref _communicationInstance, value);
                    RaisePropertyChanged(nameof(InstanceInfo));
                    RaisePropertyChanged(nameof(OutputInfo));
                    // 同步更新输出连接器的 Value
                    if (Outputs.Count > 0)
                        Outputs[0].Value = value;
                }
            }
        }

        /// <summary>是否已连接</summary>
        public bool IsConnected
        {
            get => _isConnected;
            set
            {
                SetProperty(ref _isConnected, value);

                RaisePropertyChanged(nameof(InstanceInfo));
                RaisePropertyChanged(nameof(OutputInfo));
            }
        }

        /// <summary>连接检测间隔（毫秒），默认 5000ms</summary>
        public int CheckIntervalMs
        {
            get => _checkIntervalMs;
            set
            {
                if (_checkIntervalMs != value && value >= 1000)
                {
                    SetProperty(ref _checkIntervalMs, value);
                    RestartCheckTimer();
                }
            }
        }

        /// <summary>状态消息（IExecutableOperation 接口要求）</summary>
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
        /// 通讯实例信息（节点内容区显示）
        /// 执行后展示 Instance 的关键内容
        /// </summary>
        public string InstanceInfo
        {
            get
            {
                if (CommunicationInstance == null)
                    return "未创建实例";

                var info = $"类型: {CommunicationTypeDisplay}\n";
                info += $"连接: {CommunicationInstance.ConnectionString}\n";
                info += $"状态: {(IsConnected ? "已连接" : "未连接")}\n";
                info += $"Key: {CommunicationInstance.Key}\n";
                if (!string.IsNullOrEmpty(StatusMessage))
                    info += $"\n{StatusMessage}";
                return info;
            }
        }

        public override string ToString() => InstanceInfo;

        /// <summary>
        /// 输入信息（"当前数据" Tab 显示，此插件无输入返回 null）
        /// </summary>
        public string? InputInfo => null;

        /// <summary>
        /// 输出信息（"当前数据" Tab 显示，JSON 格式）
        /// </summary>
        public string? OutputInfo
        {
            get
            {
                if (CommunicationInstance == null)
                    return null;

                var sb = new System.Text.StringBuilder();
                sb.AppendLine("{");
                sb.AppendLine($"  \"类型\": \"{CommunicationTypeDisplay}\",");
                sb.AppendLine($"  \"连接字符串\": \"{CommunicationInstance.ConnectionString}\",");
                sb.AppendLine($"  \"状态\": \"{(IsConnected ? "已连接" : "未连接")}\",");
                sb.AppendLine($"  \"Key\": \"{CommunicationInstance.Key}\",");
                sb.AppendLine($"  \"远程IP\": \"{RemoteIP}\",");
                sb.AppendLine($"  \"远程端口\": {RemotePort},");
                sb.AppendLine($"  \"本地端口\": {LocalPort},");
                sb.AppendLine($"  \"超时\": {Timeout},");
                sb.AppendLine($"  \"重试次数\": {RetryCount}");
                sb.Append("}");
                return sb.ToString();
            }
        }

        #endregion

        #region 构造函数

        public TcpsCommOperationViewModel()
        {
            ApplyDefaultPorts();
        }

        /// <summary>根据通讯类型设置默认端口</summary>
        private void ApplyDefaultPorts()
        {
            switch (CommunicationType)
            {
                case eCommunicationType.TCPClient:
                    RemotePort = 9000;
                    break;
                case eCommunicationType.TCPServer:
                    LocalPort = 8000;
                    break;
                case eCommunicationType.UDP:
                    RemotePort = 9000;
                    LocalPort = 8000;
                    break;
                case eCommunicationType.Serial:
                    RemotePort = 9600;
                    break;
            }
        }

        #endregion

        #region IExecutableOperation 实现

        /// <summary>
        /// 执行通讯连接
        /// 通过 CommunicationFactory 创建通讯实例，连接成功后自动设置输出连接器的 Value
        /// 分组锁策略：相同连接参数（类型/IP/端口）必须排队，防止重复连接冲突；不同参数并行执行
        /// </summary>
        public bool Execute()
        {
            RunStatusMessage(NodeRunStatus.Disabled, string.Empty);
            // 已经连接且连接参数未变，直接返回成功（防止重复连接）
            if (IsConnected && CommunicationInstance != null)
            {
                //StatusMessage = $"已连接 {RemoteIP}:{RemotePort}";
                return true;
            }

            var lockKey = GetConnectionLockKey();
            var semaphore = GetOrCreateLock(lockKey);

            try
            {
                // 等待获取锁：相同连接参数排队，不同参数并行
                if (!semaphore.Wait(Timeout))
                {
                    RunStatusMessage(NodeRunStatus.Error, $"获取连接锁超时 {RemoteIP}:{RemotePort}");
                    return false;
                }

                // 进入锁后，再次检查是否已经连接成功（避免竞态）
                if (IsConnected && CommunicationInstance != null)
                {
                    //StatusMessage = $"已连接 {RemoteIP}:{RemotePort}";
                    return true;
                }

                // 取消旧实例的事件订阅，防止旧实例状态更新干扰当前结果
                if (CommunicationInstance != null)
                {
                    CommunicationInstance.OnStateChanged -= OnCommunicationStateChanged;
                    (CommunicationInstance as IDisposable)?.Dispose();
                    CommunicationInstance = null;
                    IsConnected = false;
                }

                var config = BuildCommunicationConfig();
                CommunicationInstance = CommunicationFactory.Create(config);

                CommunicationInstance.OnStateChanged += OnCommunicationStateChanged;

                // 使用 Task.Run 将 ConnectAsync 放到线程池执行，避免 UI 线程死锁
                var task = Task.Run(() => CommunicationInstance.ConnectAsync());
                if (!task.Wait(TimeSpan.FromMilliseconds(Timeout)))
                {
                    // 超时：检查 IsConnected 是否已被异步回调设置为 true
                    if (IsConnected)
                    {
                        //StatusMessage = $"已连接 {CommunicationInstance.ConnectionString}";
                        System.Diagnostics.Debug.WriteLine(
                            $"[TcpsCommOperationViewModel] Connected (async): {CommunicationInstance.ConnectionString}");
                        Outputs[0].Value = CommunicationInstance;
                        StartCheckTimer();
                        return true;
                    }
                    RunStatusMessage(NodeRunStatus.Error, $"连接超时 {RemoteIP}:{RemotePort}");
                    System.Diagnostics.Debug.WriteLine(
                        $"[TcpsCommOperationViewModel] Connection timeout: {RemoteIP}:{RemotePort}");
                    (CommunicationInstance as IDisposable)?.Dispose();
                    CommunicationInstance = null;
                    return false;
                }

                if (task.Result || IsConnected)
                {
                    //StatusMessage = $"已连接 {CommunicationInstance.ConnectionString}";
                    System.Diagnostics.Debug.WriteLine(
                        $"[TcpsCommOperationViewModel] Connected: {CommunicationInstance.ConnectionString}");
                    Outputs[0].Value = CommunicationInstance;
                    StartCheckTimer();
                    return true;
                }
                else
                {
                    RunStatusMessage(NodeRunStatus.Error, $"连接失败 {RemoteIP}:{RemotePort}（超时或拒绝）");
                    System.Diagnostics.Debug.WriteLine(
                        $"[TcpsCommOperationViewModel] Connection failed: {RemoteIP}:{RemotePort}");
                    (CommunicationInstance as IDisposable)?.Dispose();
                    CommunicationInstance = null;
                    return false;
                }
            }
            catch (Exception ex)
            {
                RunStatusMessage(NodeRunStatus.Error, $"连接异常: {ex.Message}");
                System.Diagnostics.Debug.WriteLine($"[TcpsCommOperationViewModel] Error: {ex.Message}");
                return false;
            }
            finally
            {
                semaphore.Release();
            }
        }

        /// <summary>通讯状态变化回调（提取为命名方法，便于取消订阅）</summary>
        private void OnCommunicationStateChanged(object? sender, CommunicationState state)
        {
            IsConnected = state == CommunicationState.Connected;
            if (IsConnected)
            {
                StartCheckTimer();
            }
            else
            {
                StopCheckTimer();
            }
        }

        /// <summary>断开通讯连接（使用分组锁防止与 Execute 竞争）</summary>
        public bool Disconnect()
        {
            if (CommunicationInstance == null) return true;

            var lockKey = GetConnectionLockKey();
            var semaphore = GetOrCreateLock(lockKey);

            try
            {
                if (!semaphore.Wait(TimeSpan.FromMilliseconds(Timeout)))
                    return false;

                if (CommunicationInstance == null) return true;

                // 使用 Task.Run 避免 UI 线程死锁
                var task = Task.Run(() => CommunicationInstance.DisconnectAsync());
                task.Wait(TimeSpan.FromMilliseconds(Timeout));
                CommunicationInstance.OnStateChanged -= OnCommunicationStateChanged;
                (CommunicationInstance as IDisposable)?.Dispose();
                CommunicationInstance = null;
                IsConnected = false;
                //StatusMessage = "已断开连接";
                StopCheckTimer();
                return true;
            }
            catch (Exception ex)
            {
                StatusMessage = $"断开异常: {ex.Message}";
                return false;
            }
            finally
            {
                semaphore.Release();
            }
        }

        #region 连接状态主动检测

        /// <summary>
        /// 启动连接状态检测定时器
        /// 连接成功后调用，定期通过 CheckConnectionAsync 主动探测连接是否存活
        /// </summary>
        private void StartCheckTimer()
        {
            StopCheckTimer();
            _connectionCheckTimer = new Timer(OnCheckTimerCallback, null, _checkIntervalMs, _checkIntervalMs);
            System.Diagnostics.Debug.WriteLine(
                $"[TcpsCommOperationViewModel] 连接检测定时器已启动，间隔 {_checkIntervalMs}ms");
        }

        /// <summary>停止连接状态检测定时器</summary>
        private void StopCheckTimer()
        {
            if (_connectionCheckTimer != null)
            {
                _connectionCheckTimer.Dispose();
                _connectionCheckTimer = null;
                System.Diagnostics.Debug.WriteLine("[TcpsCommOperationViewModel] 连接检测定时器已停止");
            }
        }

        /// <summary>重启连接状态检测定时器（检测间隔变更时调用）</summary>
        private void RestartCheckTimer()
        {
            if (_connectionCheckTimer != null && IsConnected)
            {
                StartCheckTimer();
            }
        }

        /// <summary>
        /// 定时器回调：主动检测连接状态
        /// 如果检测到连接断开，更新 IsConnected 和 StatusMessage
        /// </summary>
        private async void OnCheckTimerCallback(object? state)
        {
            try
            {
                if (CommunicationInstance == null || !IsConnected)
                {
                    StopCheckTimer();
                    return;
                }

                var isAlive = await CommunicationInstance.CheckConnectionAsync();
                if (!isAlive)
                {
                    System.Diagnostics.Debug.WriteLine(
                        $"[TcpsCommOperationViewModel] 主动检测到连接已断开: {CommunicationInstance.ConnectionString}");
                    IsConnected = false;
                    StatusMessage = "连接已断开（主动检测）";
                    StopCheckTimer();
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine(
                    $"[TcpsCommOperationViewModel] 连接检测异常: {ex.Message}");
                // 检测异常也视为连接断开
                IsConnected = false;
                StatusMessage = $"连接异常: {ex.Message}";
                StopCheckTimer();
            }
        }

        /// <summary>
        /// 手动执行一次连接检测（可被外部调用）
        /// </summary>
        public async Task<bool> CheckConnectionNowAsync()
        {
            if (CommunicationInstance == null) return false;
            try
            {
                var isAlive = await CommunicationInstance.CheckConnectionAsync();
                if (!isAlive && IsConnected)
                {
                    IsConnected = false;
                    StatusMessage = "连接已断开（手动检测）";
                    StopCheckTimer();
                }
                return isAlive;
            }
            catch
            {
                if (IsConnected)
                {
                    IsConnected = false;
                    StatusMessage = "连接检测失败";
                    StopCheckTimer();
                }
                return false;
            }
        }

        #endregion

        /// <summary>构建 CommunicationConfig</summary>
        private CommunicationConfig BuildCommunicationConfig()
        {
            return new CommunicationConfig
            {
                Key = $"TCPComm_{DateTime.Now:yyyyMMddHHmmss}",
                Name = "TCP 通讯",
                CommunicationType = CommunicationType,
                RemoteIP = RemoteIP,
                RemotePort = RemotePort,
                LocalPort = LocalPort,
                Timeout = Timeout,
                RetryCount = RetryCount,
                IsSendByHex = IsSendByHex,
                IsReceivedByHex = IsReceivedByHex,
                // OPC UA 专用：根据 RemoteIP/RemotePort 动态构建 URL
                OpcUaServerUrl = $"opc.tcp://{RemoteIP}:{RemotePort}",
                OpcUaPort = RemotePort,
                // WebSocket 专用
                WebSocketUrl = $"ws://{RemoteIP}:{RemotePort}/ws",
                WebSocketPort = RemotePort,
                // HTTP 专用
                HttpBaseUrl = $"http://{RemoteIP}:{RemotePort}",
                HttpBasePort = RemotePort,
            };
        }

        #endregion

        #region ISerializableOperation 实现
        /// <summary>序列化版本号（兼容不同版本的反序列化）</summary>
        private const int SerializeVersion = 2;
        /// <summary>
        /// 将插件参数序列化为二进制
        /// 格式：Version(int) → Title(string) → CommunicationType(int) → RemoteIP(string) →
        ///       RemotePort(int) → LocalPort(int) → Timeout(int) → RetryCount(int) →
        ///       IsSendByHex(bool) → IsReceivedByHex(bool) → PresetName(string)
        /// </summary>
        public void Serialize(BinaryWriter writer)
        {
            writer.Write(SerializeVersion);
            writer.Write(Title);
            writer.Write((int)CommunicationType);
            writer.Write(RemoteIP);
            writer.Write(RemotePort);
            writer.Write(LocalPort);
            writer.Write(Timeout);
            writer.Write(RetryCount);
            writer.Write(IsSendByHex);
            writer.Write(IsReceivedByHex);
            // v2: 保存预置协议名称，反序列化时恢复 ComboBox 选中状态
            writer.Write(SelectedPreset?.Name ?? "");
        }

        /// <summary>
        /// 从二进制反序列化插件参数
        /// </summary>
        public void Deserialize(BinaryReader reader)
        {
            var version = reader.ReadInt32();
            Title = reader.ReadString();
            var commType = (eCommunicationType)reader.ReadInt32();
            var remoteIP = reader.ReadString();
            var remotePort = reader.ReadInt32();
            var localPort = reader.ReadInt32();
            var timeout = reader.ReadInt32();
            var retryCount = reader.ReadInt32();
            var isSendByHex = reader.ReadBoolean();
            var isReceivedByHex = reader.ReadBoolean();

            // v2+: 读取预置协议名称
            var presetName = version >= 2 ? reader.ReadString() : "";

            // 先恢复预置协议（自动填充参数），再恢复个体参数（覆盖用户自定义值）
            if (!string.IsNullOrEmpty(presetName))
            {
                var preset = ProtocolConfig.PresetProtocols
                    .FirstOrDefault(p => p.Name == presetName);
                if (preset != null)
                {
                    _selectedPreset = preset;
                    RaisePropertyChanged(nameof(SelectedPreset));
                }
            }

            // 个体参数覆盖预置值（保留用户自定义修改）
            CommunicationType = commType;
            RemoteIP = remoteIP;
            RemotePort = remotePort;
            LocalPort = localPort;
            Timeout = timeout;
            RetryCount = retryCount;
            IsSendByHex = isSendByHex;
            IsReceivedByHex = isReceivedByHex;
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
