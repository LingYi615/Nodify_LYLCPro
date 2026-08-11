using System;
using System.Threading.Tasks;

namespace LYCorePro.Communacation
{
    /// <summary>
    /// 通讯状态枚举
    /// </summary>
    public enum CommunicationState
    {
        /// <summary>已断开</summary>
        Disconnected,
        /// <summary>正在连接</summary>
        Connecting,
        /// <summary>已连接</summary>
        Connected,
        /// <summary>错误</summary>
        Error
    }

    /// <summary>
    /// 通讯接口 - 定义所有通讯实现必须遵循的契约
    /// </summary>
    public interface ICommunication
    {
        /// <summary>收到消息事件</summary>
        event EventHandler<string> OnMessageReceived;
        /// <summary>状态变化事件</summary>
        event EventHandler<CommunicationState> OnStateChanged;
        /// <summary>错误事件</summary>
        event EventHandler<Exception> OnErrorOccurred;

        /// <summary>当前状态</summary>
        CommunicationState State { get; }
        /// <summary>是否已连接</summary>
        bool IsConnected { get; }
        /// <summary>通讯唯一标识 Key</summary>
        string Key { get; }
        /// <summary>连接字符串（用于调试和日志）</summary>
        string ConnectionString { get; }
        /// <summary>通讯配置</summary>
        CommunicationConfig Config { get; }

        /// <summary>异步连接</summary>
        Task<bool> ConnectAsync();
        /// <summary>异步断开</summary>
        Task<bool> DisconnectAsync();
        /// <summary>发送字符串数据</summary>
        Task<bool> SendAsync(string data);
        /// <summary>发送字节数据</summary>
        Task<bool> SendAsync(byte[] data);
        /// <summary>接收数据</summary>
        Task<string> ReceiveAsync();
        /// <summary>泛型读取（PLC 协议专用）</summary>
        Task<T> ReadAsync<T>(string address);
        /// <summary>泛型写入（PLC 协议专用）</summary>
        Task<bool> WriteAsync<T>(string address, T value);
        /// <summary>读取字符串</summary>
        Task<string> ReadStringAsync(string address, ushort length);
        /// <summary>写入字符串</summary>
        Task<bool> WriteStringAsync(string address, string value);
        /// <summary>检测连接是否存活（主动探测）</summary>
        Task<bool> CheckConnectionAsync();
    }
}