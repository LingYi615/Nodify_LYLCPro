using System;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace LYCorePro.Communacation
{
    /// <summary>
    /// UDP 通讯实现
    /// 使用 UdpClient 进行无连接数据收发
    /// </summary>
    public class UdpCommunication : CommunicationBase
    {
        private UdpClient _udpClient;
        private IPEndPoint _remoteEndPoint;
        private CancellationTokenSource _cts;

        public override string ConnectionString => $"UDP://{Config.RemoteIP}:{Config.RemotePort}";

        public UdpCommunication(CommunicationConfig config) : base(config) { }

        protected override async Task<bool> ConnectCoreAsync()
        {
            try
            {
                _udpClient = new UdpClient(Config.LocalPort);
                _remoteEndPoint = new IPEndPoint(IPAddress.Parse(Config.RemoteIP), Config.RemotePort);
                _cts = new CancellationTokenSource();
                _ = ReceiveLoopAsync(_cts.Token);
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
                _cts?.Cancel();
                _udpClient?.Close();
                _udpClient?.Dispose();
                return true;
            }
            catch { return false; }
        }

        private async Task ReceiveLoopAsync(CancellationToken token)
        {
            try
            {
                while (!token.IsCancellationRequested)
                {
                    var result = await _udpClient.ReceiveAsync();
                    if (result.Buffer != null && result.Buffer.Length > 0)
                    {
                        string message = Config.IsReceivedByHex
                            ? HexTool.ToHexStringFromDataBytes(result.Buffer)
                            : Encoding.Default.GetString(result.Buffer).Trim('\0');

                        if (!string.IsNullOrEmpty(message))
                        {
                            lock (_syncLock)
                            {
                                _receiveQueue.Enqueue(message);
                                RaiseMessageReceived(message);
                                _receiveSignal.Set();
                            }
                        }
                    }
                }
            }
            catch (OperationCanceledException) { }
            catch (Exception ex) { RaiseErrorOccurred(ex); }
        }

        protected override async Task<bool> SendCoreAsync(byte[] data)
        {
            try
            {
                await _udpClient.SendAsync(data, data.Length, _remoteEndPoint);
                return true;
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
            _udpClient?.Close();
            _udpClient?.Dispose();
        }
    }
}