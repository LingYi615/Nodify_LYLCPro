using System;
using System.IO;
using System.Net.Sockets;
using System.Threading.Tasks;

namespace LYCorePro.Communacation
{
    /// <summary>
    /// TCP 客户端通讯实现
    /// 使用 TcpClient 连接远程 TCP 服务器
    /// 支持主动和被动连接断开检测
    /// </summary>
    public class TcpClientCommunication : CommunicationBase
    {
        private TcpClient _client;
        private NetworkStream _stream;

        public override string ConnectionString => $"TCP-Client://{Config.RemoteIP}:{Config.RemotePort}";

        public TcpClientCommunication(CommunicationConfig config) : base(config) { }

        protected override async Task<bool> ConnectCoreAsync()
        {
            try
            {
                _client = new TcpClient();
                await _client.ConnectAsync(Config.RemoteIP, Config.RemotePort);
                _stream = _client.GetStream();
                return true;
            }
            catch (Exception ex)
            {
                RaiseErrorOccurred(ex);
                return false;
            }
        }

        protected override async Task<bool> DisconnectCoreAsync()
        {
            try
            {
                _stream?.Close();
                _stream?.Dispose();
                _client?.Close();
                _client?.Dispose();
                return true;
            }
            catch { return false; }
        }

        /// <summary>
        /// 检测 TCP 连接是否存活
        /// 使用 Socket.Poll 进行主动探测，比 TcpClient.Connected 更可靠
        /// </summary>
        public override Task<bool> CheckConnectionAsync()
        {
            return Task.FromResult(IsSocketAlive());
        }

        /// <summary>
        /// 使用 Socket.Poll 检测 TCP 连接是否仍然存活
        /// </summary>
        private bool IsSocketAlive()
        {
            try
            {
                if (_client?.Client == null || _stream == null)
                    return false;

                var socket = _client.Client;

                // Poll(0, SelectRead) 检查 socket 是否可读
                // 如果返回 true 且 Available == 0，说明连接已断开（FIN/RST 到达）
                if (socket.Poll(0, SelectMode.SelectRead))
                {
                    return socket.Available > 0; // 可读且有数据 = 连接正常
                }

                // Poll 返回 false 表示连接正常（没有可读事件）
                return true;
            }
            catch (ObjectDisposedException)
            {
                return false;
            }
            catch (SocketException)
            {
                return false;
            }
        }

        protected override async Task<bool> SendCoreAsync(byte[] data)
        {
            try
            {
                if (_stream == null) return false;

                // 发送前检测连接状态
                if (!IsSocketAlive()) return false;

                await _stream.WriteAsync(data, 0, data.Length);
                await _stream.FlushAsync();
                return true;
            }
            catch (IOException)
            {
                return false;
            }
            catch (SocketException)
            {
                return false;
            }
            catch (ObjectDisposedException)
            {
                return false;
            }
            catch { return false; }
        }

        protected override async Task<byte[]> ReceiveCoreAsync()
        {
            try
            {
                if (_stream == null || !_stream.CanRead) return null;

                var buffer = new byte[4096];
                var bytesRead = await _stream.ReadAsync(buffer, 0, buffer.Length);
                if (bytesRead > 0)
                {
                    var data = new byte[bytesRead];
                    Array.Copy(buffer, data, bytesRead);
                    return data;
                }
                // bytesRead == 0 表示对端已关闭连接（FIN），抛出 IOException 让基类检测断开
                throw new IOException("TCP 连接已关闭（对端断开）");
            }
            catch (IOException)
            {
                throw; // 重新抛出让基类 StartReceiveLoopAsync 处理
            }
            catch (SocketException)
            {
                throw; // 重新抛出让基类处理
            }
            catch (ObjectDisposedException)
            {
                throw; // 重新抛出让基类处理
            }
            catch
            {
                return null;
            }
        }

        public override void Dispose()
        {
            base.Dispose();
            _stream?.Dispose();
            _client?.Dispose();
        }
    }
}