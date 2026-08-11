using System;
using System.Collections.Concurrent;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace LYCorePro.Communacation
{
    /// <summary>
    /// TCP 服务器通讯实现
    /// 使用 TcpListener 监听本地端口，支持多客户端连接
    /// </summary>
    public class TcpServerCommunication : CommunicationBase
    {
        private TcpListener _listener;
        private readonly ConcurrentDictionary<string, TcpClient> _clients = new ConcurrentDictionary<string, TcpClient>();
        private CancellationTokenSource _cts;
        private bool _isRunning = false;

        public override string ConnectionString => $"TCP-Server://0.0.0.0:{Config.LocalPort}";

        public TcpServerCommunication(CommunicationConfig config) : base(config) { }

        protected override async Task<bool> ConnectCoreAsync()
        {
            try
            {
                _listener = new TcpListener(IPAddress.Any, Config.LocalPort);
                _listener.Start();
                _isRunning = true;
                _cts = new CancellationTokenSource();
                _ = AcceptClientsAsync(_cts.Token);
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
                _isRunning = false;
                _cts?.Cancel();
                foreach (var client in _clients.Values)
                {
                    client?.Close();
                    client?.Dispose();
                }
                _clients.Clear();
                _listener?.Stop();
                return true;
            }
            catch { return false; }
        }

        /// <summary>
        /// 检测 TCP 服务器是否存活（监听是否运行中）
        /// </summary>
        public override Task<bool> CheckConnectionAsync()
        {
            return Task.FromResult(_isRunning && _listener != null);
        }

        private async Task AcceptClientsAsync(CancellationToken token)
        {
            while (_isRunning && !token.IsCancellationRequested)
            {
                try
                {
                    var client = await _listener.AcceptTcpClientAsync();
                    var endpoint = client.Client.RemoteEndPoint?.ToString() ?? Guid.NewGuid().ToString();
                    _clients[endpoint] = client;
                    _ = HandleClientAsync(client, endpoint, token);
                }
                catch (OperationCanceledException) { break; }
                catch { await Task.Delay(100, token); }
            }
        }

        private async Task HandleClientAsync(TcpClient client, string endpoint, CancellationToken token)
        {
            try
            {
                var stream = client.GetStream();
                var buffer = new byte[4096];
                while (!token.IsCancellationRequested && client.Connected)
                {
                    var bytesRead = await stream.ReadAsync(buffer, 0, buffer.Length, token);
                    if (bytesRead > 0)
                    {
                        var data = new byte[bytesRead];
                        Array.Copy(buffer, data, bytesRead);
                        RaiseMessageReceived($"客户端 {endpoint}: {Encoding.UTF8.GetString(data).Trim('\0')}              {DateTime.Now:yyyy-MM-dd HH:mm:ss}");
                        OnDataReceived(data);
                    }
                    else break;
                }
            }
            catch (OperationCanceledException) { }
            catch (Exception ex) { RaiseErrorOccurred(ex); }
            finally
            {
                _clients.TryRemove(endpoint, out _);
                client?.Close();
                client?.Dispose();
            }
        }

        private void OnDataReceived(byte[] data)
        {
            string message = Config.IsReceivedByHex
                ? HexTool.ToHexStringFromDataBytes(data)
                : Encoding.Default.GetString(data).Trim('\0');

            if (!string.IsNullOrEmpty(message))
            {
                lock (_syncLock)
                {
                    _receiveQueue.Enqueue(message);
                    _receiveSignal.Set();
                }
            }
        }

        protected override async Task<bool> SendCoreAsync(byte[] data)
        {
            try
            {
                if (_clients.Count == 0) return false;
                foreach (var client in _clients.Values)
                {
                    if (client.Connected)
                    {
                        var stream = client.GetStream();
                        await stream.WriteAsync(data, 0, data.Length);
                        await stream.FlushAsync();
                    }
                }
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
            return await Task.FromResult<byte[]>(null);
        }

        public override void Dispose()
        {
            base.Dispose();
            _cts?.Dispose();
            _listener?.Stop();
        }
    }
}