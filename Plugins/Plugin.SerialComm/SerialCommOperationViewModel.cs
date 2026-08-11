using System;
using System.Collections.Concurrent;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.IO.Ports;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using LYCorePro.Communacation;
using LYCorePro.Common.Enum;
using Nodify;
using PluginBase.Interfaces;
using LYCorePro.Core;
using PluginBase.Models;
using System.Windows.Threading;
using System.Windows;
using LYCorePro.Common.Helper;
using System.Diagnostics;

namespace SerialCommPlugin
{
    /// <summary>
    /// 串口通讯插件操作（节点实例）
    /// 无输入连接器，一个输出连接器（输出 ICommunication 通讯实例）
    /// 固定使用 eCommunicationType.Serial 通讯类型
    /// </summary>
    public class SerialCommOperationViewModel : NotifyPropertyBase, IExecutableOperation, ISerializableOperation
    {
        #region 字段

        private string _title = "串口通讯";
        private string _portName = "COM1";
        private int _baudRate = 9600;
        private Parity _parity = Parity.None;
        private int _dataBits = 8;
        private StopBits _stopBits = StopBits.One;
        private int _timeout = 5000;
        private int _retryCount = 3;
        private bool _isSendByHex;
        private bool _isReceivedByHex;
        private ICommunication? _communicationInstance;
        private string? _statusMessage;
        private PluginBase.Interfaces.NodeRunStatus _runStatus;

        private bool _isConnected;
        private Timer? _connectionCheckTimer;
        private int _checkIntervalMs = 5000;

        private string _icon = "M228.693333 256h598.613334c81.92 0 142.506667 75.52 125.013333 155.306667l-35.413333 160.426666a180.096 180.096 0 0 0-143.36-70.4c-100.266667 0-181.333333 81.066667-181.333334 181.333334 0 39.68 12.8 76.373333 34.56 106.666666H290.133333c-60.16 0-111.786667-41.813333-125.013333-100.693333l-61.44-277.333333A128.042667 128.042667 0 0 1 228.693333 256z m-9.813333 160a53.333333 53.333333 0 1 0 106.666667 0 53.333333 53.333333 0 0 0-106.666667 0z m170.666667 0a53.333333 53.333333 0 1 0 106.666666 0 53.333333 53.333333 0 0 0-106.666666 0z m170.666666 0a53.333333 53.333333 0 1 0 106.666667 0 53.333333 53.333333 0 0 0-106.666667 0z m170.666667 0a53.333333 53.333333 0 1 0 106.666667 0 53.333333 53.333333 0 0 0-106.666667 0z m-426.666667 192a53.333333 53.333333 0 1 0 106.666667 0 53.333333 53.333333 0 0 0-106.666667 0z m170.666667 0a53.333333 53.333333 0 1 0 106.666667 0 53.333333 53.333333 0 0 0-106.666667 0zM892.586667 649.813333l-16.64-2.133333a11.050667 11.050667 0 0 1-8.106667-6.826667c-3.413333-8.106667-7.68-15.786667-12.8-22.613333-2.133333-2.986667-2.986667-7.253333-1.28-10.666667l6.4-16.64c1.706667-4.693333 0-10.666667-4.693333-13.226666l-35.84-21.76a10.581333 10.581333 0 0 0-13.653334 2.56l-10.24 14.08c-2.133333 2.986667-5.973333 4.266667-9.813333 3.84-3.84-0.426667-8.106667-0.853333-12.373333-0.853334-4.266667 0-8.533333 0.426667-12.373334 0.853334-3.84 0.426667-7.68-0.853333-9.813333-3.84l-10.666667-14.08c-2.986667-4.266667-8.533333-5.12-13.226666-2.56l-36.266667 21.76c-4.266667 2.56-5.973333 8.533333-4.266667 13.226666l6.4 16.64c1.28 3.413333 0.853333 7.253333-1.706666 10.666667-4.693333 6.826667-8.96 14.506667-12.373334 22.613333-1.28 3.413333-4.266667 5.973333-8.106666 6.826667l-16.64 2.133333a11.093333 11.093333 0 0 0-8.96 11.093334v43.52c0 5.12 3.84 9.813333 8.96 10.666666l16.64 2.133334c3.413333 0.853333 6.826667 3.413333 8.106666 6.826666 3.413333 8.106667 7.68 15.786667 12.373334 22.613334 2.56 3.413333 2.986667 7.253333 1.706666 10.666666l-6.4 16.64c-2.133333 4.693333 0 10.666667 4.266667 13.226667l36.266667 21.76c4.693333 2.56 10.24 1.706667 13.226666-2.56l10.666667-14.08c2.133333-2.986667 5.973333-4.266667 9.386667-3.84 4.266667 0.426667 8.533333 0.853333 12.8 0.853333 4.266667 0 8.106667-0.426667 12.373333-0.853333 3.84-0.426667 7.68 0.853333 9.813333 3.84l10.24 14.08c3.413333 4.266667 8.96 5.12 13.226667 2.56l36.266667-21.76c4.693333-2.56 6.4-8.533333 4.266666-13.226667l-5.973333-16.64c-1.706667-3.413333-0.853333-7.253333 1.28-10.666666 5.12-6.826667 9.386667-14.506667 12.8-22.613334 1.28-3.413333 4.266667-5.973333 8.106667-6.826666l16.64-2.133334c5.12-0.853333 8.96-5.546667 8.96-10.666666v-43.52a11.093333 11.093333 0 0 0-8.96-11.093334z m-119.04 75.093334c-22.613333 0-40.533333-19.2-40.533334-42.24 0-23.466667 17.92-42.666667 40.533334-42.666667 22.186667 0 40.533333 19.2 40.533333 42.666667 0 23.04-18.346667 42.24-40.533333 42.24z";


        #endregion

        #region 连接分组锁管理器（静态）

        private static readonly ConcurrentDictionary<string, SemaphoreSlim>
            _connectionLocks = new();

        private string GetConnectionLockKey()
        {
            return $"Serial_{PortName}_{BaudRate}";
        }

        private static SemaphoreSlim GetOrCreateLock(string key)
        {
            return _connectionLocks.GetOrAdd(key, _ => new SemaphoreSlim(1, 1));
        }

        #endregion

        #region 静态资源

        /// <summary>可用串口列表（供参数面板 ComboBox 绑定）</summary>
        public static string[] PortNames => SerialPort.GetPortNames();

        /// <summary>常用波特率列表</summary>
        public static int[] BaudRates { get; } = { 1200, 2400, 4800, 9600, 14400, 19200, 38400, 56000, 57600, 115200, 230400, 460800 };

        /// <summary>校验位选项</summary>
        public static Parity[] ParityOptions { get; } = (Parity[])Enum.GetValues(typeof(Parity));

        /// <summary>数据位选项</summary>
        public static int[] DataBitsOptions { get; } = { 5, 6, 7, 8 };

        /// <summary>停止位选项</summary>
        public static StopBits[] StopBitsOptions { get; } = (StopBits[])Enum.GetValues(typeof(StopBits));

        #endregion

        #region 节点属性（供 OperationNodeViewModel 反射读取）

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

        public ObservableCollection<ConnectorViewModel> Outputs { get; } = new()
        {
            new ConnectorViewModel
            {
                Title = "Instance",
                DataType = DataType.SerialAgreement,
                Direction = ConnectorDirection.Output
            }
        };

        #endregion

        #region 串口参数属性

        /// <summary>串口号</summary>
        public string PortName
        {
            get => _portName;
            set { SetProperty(ref _portName, value); RaisePropertyChanged(nameof(InstanceInfo)); RaisePropertyChanged(nameof(OutputInfo)); }
        }

        /// <summary>波特率</summary>
        public int BaudRate
        {
            get => _baudRate;
            set { SetProperty(ref _baudRate, value); RaisePropertyChanged(nameof(InstanceInfo)); RaisePropertyChanged(nameof(OutputInfo)); }
        }

        /// <summary>校验位</summary>
        public Parity Parity
        {
            get => _parity;
            set { SetProperty(ref _parity, value); RaisePropertyChanged(nameof(InstanceInfo)); RaisePropertyChanged(nameof(OutputInfo)); }
        }

        /// <summary>数据位</summary>
        public int DataBits
        {
            get => _dataBits;
            set { SetProperty(ref _dataBits, value); ; RaisePropertyChanged(nameof(InstanceInfo)); RaisePropertyChanged(nameof(OutputInfo)); }
        }

        /// <summary>停止位</summary>
        public StopBits StopBits
        {
            get => _stopBits;
            set { SetProperty(ref _stopBits, value); RaisePropertyChanged(nameof(InstanceInfo)); RaisePropertyChanged(nameof(OutputInfo)); }
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
                    if (Outputs.Count > 0)
                        Outputs[0].Value = value;
                }
            }
        }

        public bool IsConnected
        {
            get => _isConnected;
            set
            {
                SetProperty(ref  _isConnected, value);
                RaisePropertyChanged(nameof(InstanceInfo));
                RaisePropertyChanged(nameof(OutputInfo));
            }
        }

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

        public string InstanceInfo
        {
            get
            {
                if (CommunicationInstance == null)
                    return "未创建实例";

                var info = $"类型: 串口通讯\n";
                info += $"端口: {PortName} {BaudRate},{DataBits},{Parity},{StopBits}\n";
                info += $"状态: {(IsConnected ? "已连接" : "未连接")}\n";
                info += $"Key: {CommunicationInstance.Key}\n";
                if (!string.IsNullOrEmpty(StatusMessage))
                    info += $"\n{StatusMessage}";
                return info;
            }
        }

        public override string ToString() => InstanceInfo;

        public string? InputInfo => null;

        public string? OutputInfo
        {
            get
            {
                if (CommunicationInstance == null)
                    return null;

                var sb = new System.Text.StringBuilder();
                sb.AppendLine("{");
                sb.AppendLine($"  \"类型\": \"串口通讯\",");
                sb.AppendLine($"  \"端口\": \"{PortName}\",");
                sb.AppendLine($"  \"波特率\": {BaudRate},");
                sb.AppendLine($"  \"校验位\": \"{Parity}\",");
                sb.AppendLine($"  \"数据位\": {DataBits},");
                sb.AppendLine($"  \"停止位\": \"{StopBits}\",");
                sb.AppendLine($"  \"状态\": \"{(IsConnected ? "已连接" : "未连接")}\",");
                sb.AppendLine($"  \"Key\": \"{CommunicationInstance.Key}\",");
                sb.AppendLine($"  \"超时\": {Timeout},");
                sb.AppendLine($"  \"重试次数\": {RetryCount}");
                sb.Append("}");
                return sb.ToString();
            }
        }

        #endregion

        #region IExecutableOperation 实现

        public bool Execute()
        {
            RunStatusMessage(NodeRunStatus.Disabled, string.Empty);
            if (IsConnected && CommunicationInstance != null)
            {
                return true;
            }

            var lockKey = GetConnectionLockKey();
            var semaphore = GetOrCreateLock(lockKey);

            try
            {
                if (!semaphore.Wait(Timeout))
                {
                    RunStatusMessage(NodeRunStatus.Error, $"获取连接锁超时 {PortName}");
                    return false;
                }

                if (IsConnected && CommunicationInstance != null)
                {
                    return true;
                }

                if (CommunicationInstance != null)
                {
                    CommunicationInstance.OnStateChanged -= OnCommunicationStateChanged;
                    (CommunicationInstance as IDisposable)?.Dispose();
                    CommunicationInstance = null;
                    IsConnected = false;
                }

                var config = new CommunicationConfig
                {
                    Key = $"SerialComm_{DateTime.Now:yyyyMMddHHmmss}",
                    Name = "串口通讯",
                    CommunicationType = eCommunicationType.Serial,
                    PortName = PortName,
                    BaudRate = BaudRate,
                    Parity = Parity,
                    DataBits = DataBits,
                    StopBits = StopBits,
                    Timeout = Timeout,
                    RetryCount = RetryCount,
                    IsSendByHex = IsSendByHex,
                    IsReceivedByHex = IsReceivedByHex,
                };

                CommunicationInstance = CommunicationFactory.Create(config);
                CommunicationInstance.OnStateChanged += OnCommunicationStateChanged;

                var task = Task.Run(() => CommunicationInstance.ConnectAsync());
                if (!task.Wait(TimeSpan.FromMilliseconds(Timeout)))
                {
                    if (IsConnected)
                    {
                        Outputs[0].Value = CommunicationInstance;
                        StartCheckTimer();
                        return true;
                    }
                    RunStatusMessage(NodeRunStatus.Error, $"连接超时 {PortName}");
                    (CommunicationInstance as IDisposable)?.Dispose();
                    CommunicationInstance = null;
                    return false;
                }

                if (task.Result || IsConnected)
                {
                    Outputs[0].Value = CommunicationInstance;
                    StartCheckTimer();
                    return true;
                }
                else
                {
                    RunStatusMessage(NodeRunStatus.Error, $"连接失败 {PortName}");
                    (CommunicationInstance as IDisposable)?.Dispose();
                    CommunicationInstance = null;
                    return false;
                }
            }
            catch (Exception ex)
            {
                RunStatusMessage(NodeRunStatus.Error, $"连接异常: {ex.Message}");
                return false;
            }
            finally
            {
                semaphore.Release();
            }
        }

        private void OnCommunicationStateChanged(object? sender, CommunicationState state)
        {
            IsConnected = state == CommunicationState.Connected;
            if (IsConnected)
                StartCheckTimer();
            else
                StopCheckTimer();
        }

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

                var task = Task.Run(() => CommunicationInstance.DisconnectAsync());
                task.Wait(TimeSpan.FromMilliseconds(Timeout));
                CommunicationInstance.OnStateChanged -= OnCommunicationStateChanged;
                (CommunicationInstance as IDisposable)?.Dispose();
                CommunicationInstance = null;
                IsConnected = false;
                StatusMessage = "已断开连接";
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

        #endregion

        #region 连接状态主动检测

        private void StartCheckTimer()
        {
            StopCheckTimer();
            _connectionCheckTimer = new Timer(OnCheckTimerCallback, null, _checkIntervalMs, _checkIntervalMs);
        }

        private void StopCheckTimer()
        {
            if (_connectionCheckTimer != null)
            {
                _connectionCheckTimer.Dispose();
                _connectionCheckTimer = null;
            }
        }

        private void RestartCheckTimer()
        {
            if (_connectionCheckTimer != null && IsConnected)
                StartCheckTimer();
        }

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
                    IsConnected = false;
                    StatusMessage = "连接已断开（主动检测）";
                    StopCheckTimer();
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[SerialCommOperationViewModel] 连接检测异常: {ex.Message}");
                IsConnected = false;
                StatusMessage = $"连接异常: {ex.Message}";
                StopCheckTimer();
            }
        }

        #endregion

        #region ISerializableOperation 实现

        private const int SerializeVersion = 1;

        public void Serialize(BinaryWriter writer)
        {
            writer.Write(SerializeVersion);
            writer.Write(Title);
            writer.Write(PortName);
            writer.Write(BaudRate);
            writer.Write((int)Parity);
            writer.Write(DataBits);
            writer.Write((int)StopBits);
            writer.Write(Timeout);
            writer.Write(RetryCount);
            writer.Write(IsSendByHex);
            writer.Write(IsReceivedByHex);
        }

        public void Deserialize(BinaryReader reader)
        {
            var version = reader.ReadInt32();
            Title = reader.ReadString();
            PortName = reader.ReadString();
            BaudRate = reader.ReadInt32();
            Parity = (Parity)reader.ReadInt32();
            DataBits = reader.ReadInt32();
            StopBits = (StopBits)reader.ReadInt32();
            Timeout = reader.ReadInt32();
            RetryCount = reader.ReadInt32();
            IsSendByHex = reader.ReadBoolean();
            IsReceivedByHex = reader.ReadBoolean();
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