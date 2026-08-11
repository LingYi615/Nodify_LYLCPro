using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Sockets;
using System.Reflection;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace LYCorePro.Communacation
{
    /// <summary>
    /// 通讯抽象基类 - 实现 ICommunication 接口的公共功能
    /// 所有通讯协议实现类（TCP/UDP/串口/Modbus/S7等）均继承此类
    /// </summary>
    public abstract class CommunicationBase : ICommunication, IDisposable
    {
        #region 字段和属性

        private CommunicationState _state = CommunicationState.Disconnected;
        protected readonly object _syncLock = new object();
        protected readonly Queue<string> _receiveQueue = new Queue<string>();
        protected readonly AutoResetEvent _receiveSignal = new AutoResetEvent(false);
        protected bool _isReceiving = false;
        protected CancellationTokenSource _cts;

        /// <summary>收到消息事件</summary>
        public event EventHandler<string> OnMessageReceived;
        /// <summary>状态变化事件</summary>
        public event EventHandler<CommunicationState> OnStateChanged;
        /// <summary>错误事件</summary>
        public event EventHandler<Exception> OnErrorOccurred;

        /// <summary>当前通讯状态</summary>
        public CommunicationState State
        {
            get => _state;
            protected set
            {
                if (_state != value)
                {
                    _state = value;
                    OnStateChanged?.Invoke(this, value);
                }
            }
        }

        /// <summary>是否已连接</summary>
        public bool IsConnected => State == CommunicationState.Connected;

        /// <summary>通讯唯一标识 Key</summary>
        public string Key { get; protected set; }

        /// <summary>连接字符串（子类实现）</summary>
        public abstract string ConnectionString { get; }

        /// <summary>通讯配置</summary>
        public CommunicationConfig Config { get; protected set; }

        #endregion

        #region 构造函数

        protected CommunicationBase(CommunicationConfig config)
        {
            Config = config ?? throw new ArgumentNullException(nameof(config));
            Key = config.Key;
        }

        #endregion

        #region 抽象方法（子类必须实现）

        /// <summary>核心连接逻辑</summary>
        protected abstract Task<bool> ConnectCoreAsync();

        /// <summary>核心断开逻辑</summary>
        protected abstract Task<bool> DisconnectCoreAsync();

        /// <summary>核心发送逻辑</summary>
        protected abstract Task<bool> SendCoreAsync(byte[] data);

        /// <summary>核心接收逻辑</summary>
        protected abstract Task<byte[]> ReceiveCoreAsync();

        #endregion

        #region 公共方法

        /// <summary>
        /// 异步连接 - 成功后自动启动接收循环
        /// </summary>
        public async Task<bool> ConnectAsync()
        {
            if (IsConnected) return true;
            if (State == CommunicationState.Connecting)
                throw new InvalidOperationException("正在连接中...");

            State = CommunicationState.Connecting;

            try
            {
                var result = await ConnectCoreAsync();
                if (result)
                {
                    State = CommunicationState.Connected;
                    _cts = new CancellationTokenSource();
                    _ = StartReceiveLoopAsync(_cts.Token);
                }
                else
                {
                    State = CommunicationState.Error;
                }
                return result;
            }
            catch (Exception ex)
            {
                State = CommunicationState.Error;
                RaiseErrorOccurred(ex);
                return false;
            }
        }

        /// <summary>
        /// 异步断开连接
        /// </summary>
        public async Task<bool> DisconnectAsync()
        {
            if (!IsConnected) return true;

            try
            {
                _cts?.Cancel();
                var result = await DisconnectCoreAsync();
                State = CommunicationState.Disconnected;
                return result;
            }
            catch (Exception ex)
            {
                RaiseErrorOccurred(ex);
                return false;
            }
        }

        /// <summary>
        /// 发送字符串数据（根据配置自动处理 Hex 转换）
        /// </summary>
        public async Task<bool> SendAsync(string data)
        {
            if (!IsConnected) return false;
            var bytes = Config.IsSendByHex ? HexTool.ToBytesFromHexString(data) : Encoding.Default.GetBytes(data);
            return await SendAsync(bytes);
        }

        /// <summary>
        /// 发送字节数据（发送失败时自动检测连接状态）
        /// </summary>
        public async Task<bool> SendAsync(byte[] data)
        {
            if (!IsConnected) return false;

            try
            {
                var result = await SendCoreAsync(data);
                if (!result)
                {
                    // 发送失败，检测连接是否已断开
                    var isAlive = await CheckConnectionAsync();
                    if (!isAlive && IsConnected)
                    {
                        System.Diagnostics.Debug.WriteLine(
                            $"[CommunicationBase] 发送失败，检测到连接已断开: {ConnectionString}");
                        State = CommunicationState.Disconnected;
                    }
                }
                return result;
            }
            catch (IOException)
            {
                System.Diagnostics.Debug.WriteLine(
                    $"[CommunicationBase] 发送 IO 异常，连接已断开: {ConnectionString}");
                if (IsConnected) State = CommunicationState.Disconnected;
                return false;
            }
            catch (SocketException)
            {
                System.Diagnostics.Debug.WriteLine(
                    $"[CommunicationBase] 发送 Socket 异常，连接已断开: {ConnectionString}");
                if (IsConnected) State = CommunicationState.Disconnected;
                return false;
            }
            catch (Exception ex)
            {
                RaiseErrorOccurred(ex);
                return false;
            }
        }

        /// <summary>
        /// 从接收队列中获取数据
        /// </summary>
        public async Task<string> ReceiveAsync()
        {
            return await Task.Run(() =>
            {
                lock (_syncLock)
                {
                    if (_receiveQueue.Count > 0)
                        return _receiveQueue.Dequeue();

                    _receiveSignal.WaitOne(Config.Timeout);
                    return _receiveQueue.Count > 0 ? _receiveQueue.Dequeue() : "";
                }
            });
        }

        /// <summary>
        /// 检测连接是否存活（默认实现：返回当前 IsConnected 状态）
        /// 子类可重写以实现协议特定的连接探测（如 TCP Socket.Poll）
        /// </summary>
        public virtual Task<bool> CheckConnectionAsync()
        {
            return Task.FromResult(IsConnected);
        }

        /// <summary>
        /// 操作失败时检测连接状态并更新
        /// 子类在 Read/Write/Send 操作失败后调用此方法，自动检测并标记断开
        /// </summary>
        protected async Task HandleOperationFailureAsync()
        {
            var isAlive = await CheckConnectionAsync();
            if (!isAlive && IsConnected)
            {
                System.Diagnostics.Debug.WriteLine(
                    $"[CommunicationBase] 操作失败，检测到连接已断开: {ConnectionString}");
                State = CommunicationState.Disconnected;
            }
        }

        /// <summary>
        /// 通过反射访问 HslCommunication 客户端内部的 Socket 对象，检测 TCP 连接是否存活
        /// HslCommunication 的 Socket 属性为 protected，无法直接访问，需通过反射获取
        /// </summary>
        protected static bool IsHslSocketAlive(object hslClient)
        {
            try
            {
                if (hslClient == null) return false;
                var type = hslClient.GetType();
                var socketProp = type.GetProperty("Socket",
                    BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
                if (socketProp == null) return false;
                var socket = socketProp.GetValue(hslClient) as Socket;
                if (socket == null) return false;
                if (socket.Poll(0, SelectMode.SelectRead))
                    return socket.Available > 0;
                return true;
            }
            catch { return false; }
        }

        /// <summary>
        /// 启动接收循环（后台持续接收数据）
        /// 检测到连接断开时自动更新状态为 Disconnected
        /// </summary>
        protected virtual async Task StartReceiveLoopAsync(CancellationToken token)
        {
            _isReceiving = true;

            try
            {
                while (!token.IsCancellationRequested && IsConnected)
                {
                    byte[]? data;
                    try
                    {
                        data = await ReceiveCoreAsync();
                    }
                    catch (IOException)
                    {
                        // IO 异常表示连接已断开
                        System.Diagnostics.Debug.WriteLine(
                            $"[CommunicationBase] IO 异常，连接已断开: {ConnectionString}");
                        State = CommunicationState.Disconnected;
                        break;
                    }
                    catch (SocketException)
                    {
                        // Socket 异常表示连接已断开
                        System.Diagnostics.Debug.WriteLine(
                            $"[CommunicationBase] Socket 异常，连接已断开: {ConnectionString}");
                        State = CommunicationState.Disconnected;
                        break;
                    }
                    catch (ObjectDisposedException)
                    {
                        // 对象已释放，连接已断开
                        System.Diagnostics.Debug.WriteLine(
                            $"[CommunicationBase] 对象已释放，连接已断开: {ConnectionString}");
                        State = CommunicationState.Disconnected;
                        break;
                    }

                    if (data != null && data.Length > 0)
                    {
                        string message = Config.IsReceivedByHex
                            ? HexTool.ToHexStringFromDataBytes(data)
                            : Encoding.Default.GetString(data).Trim('\0');

                        if (!string.IsNullOrEmpty(message))
                        {
                            lock (_syncLock)
                            {
                                _receiveQueue.Enqueue(message);
                                RaiseMessageReceived(message + "              " + DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"));
                                _receiveSignal.Set();
                            }
                        }
                    }
                    else
                    {
                        await Task.Delay(10, token);
                    }
                }
            }
            catch (OperationCanceledException)
            {
                // 正常取消
            }
            catch (Exception ex)
            {
                RaiseErrorOccurred(ex);
                State = CommunicationState.Error;
            }
            finally
            {
                _isReceiving = false;
            }
        }

        #endregion

        #region 受保护方法 - 用于触发事件

        /// <summary>
        /// 数据接收时的内部处理
        /// </summary>
        protected virtual void OnDataReceived(byte[] data)
        {
            var message = System.Text.Encoding.UTF8.GetString(data);
            OnMessageReceived?.Invoke(this, message);
        }

        /// <summary>
        /// 触发错误事件（子类可调用）
        /// </summary>
        protected virtual void RaiseErrorOccurred(Exception ex)
        {
            OnErrorOccurred?.Invoke(this, ex);
        }

        /// <summary>
        /// 触发消息接收事件（子类可调用）
        /// </summary>
        protected virtual void RaiseMessageReceived(string message)
        {
            OnMessageReceived?.Invoke(this, message);
        }

        /// <summary>
        /// 触发状态变化事件（子类可调用）
        /// </summary>
        protected virtual void RaiseStateChanged(CommunicationState state)
        {
            OnStateChanged?.Invoke(this, state);
        }

        #endregion

        #region 读写方法（子类可重写 - PLC 协议专用）

        public virtual Task<T> ReadAsync<T>(string address)
        {
            throw new NotSupportedException($"协议 {GetType().Name} 不支持泛型读取");
        }

        public virtual Task<bool> WriteAsync<T>(string address, T value)
        {
            throw new NotSupportedException($"协议 {GetType().Name} 不支持泛型写入");
        }

        public virtual Task<string> ReadStringAsync(string address, ushort length)
        {
            throw new NotSupportedException($"协议 {GetType().Name} 不支持字符串读取");
        }

        public virtual Task<bool> WriteStringAsync(string address, string value)
        {
            throw new NotSupportedException($"协议 {GetType().Name} 不支持字符串写入");
        }

        #endregion

        #region IDisposable

        public virtual void Dispose()
        {
            _cts?.Cancel();
            _cts?.Dispose();
            DisconnectAsync().Wait();
        }

        #endregion
    }

    /// <summary>
    /// 十六进制工具类 - 提供 Hex 字符串与字节数组的互相转换
    /// </summary>
    public static class HexTool
    {
        /// <summary>
        /// 十六进制字符串转字节数组
        /// 例: "AB CD 12" -> {0xAB, 0xCD, 0x12}
        /// </summary>
        public static byte[] ToBytesFromHexString(string hex)
        {
            if (string.IsNullOrEmpty(hex)) return new byte[0];
            hex = hex.Replace(" ", "").Replace("-", "");
            if (hex.Length % 2 != 0) hex = "0" + hex;

            byte[] bytes = new byte[hex.Length / 2];
            for (int i = 0; i < bytes.Length; i++)
            {
                bytes[i] = Convert.ToByte(hex.Substring(i * 2, 2), 16);
            }
            return bytes;
        }

        /// <summary>
        /// 字节数组转十六进制字符串
        /// 例: {0xAB, 0xCD, 0x12} -> "AB CD 12"
        /// </summary>
        public static string ToHexStringFromDataBytes(byte[] bytes)
        {
            if (bytes == null || bytes.Length == 0) return "";
            return BitConverter.ToString(bytes).Replace("-", " ");
        }

        /// <summary>
        /// 普通字符串转十六进制字符串
        /// </summary>
        public static string StrToHexStr(string str)
        {
            if (string.IsNullOrEmpty(str)) return "";
            byte[] bytes = Encoding.Default.GetBytes(str);
            return ToHexStringFromDataBytes(bytes);
        }
    }
}