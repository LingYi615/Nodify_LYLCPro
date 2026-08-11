using System;
using System.Net.WebSockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace LYCorePro.Communacation
{
    /// <summary>
    /// WebSocket 通讯实现
    /// </summary>
    public class WebSocketCommunication : CommunicationBase
    {
        private ClientWebSocket _webSocket;
        private CancellationTokenSource _cts;

        public override string ConnectionString => $"WS://{Config.WebSocketUrl}";

        public WebSocketCommunication(CommunicationConfig config) : base(config)
        {
        }

        protected override async Task<bool> ConnectCoreAsync()
        {
            try
            {
                _webSocket = new ClientWebSocket();
                _cts = new CancellationTokenSource();

                await _webSocket.ConnectAsync(new Uri(Config.WebSocketUrl), _cts.Token);

                if (_webSocket.State == WebSocketState.Open)
                {
                    _ = ReceiveLoopAsync(_cts.Token);
                    return true;
                }
                return false;
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

                if (_webSocket != null && _webSocket.State == WebSocketState.Open)
                {
                    await _webSocket.CloseAsync(WebSocketCloseStatus.NormalClosure, "关闭连接", CancellationToken.None);
                }

                _webSocket?.Dispose();
                _webSocket = null;
                _cts?.Dispose();
                _cts = null;
                return true;
            }
            catch
            {
                return false;
            }
        }

        private async Task ReceiveLoopAsync(CancellationToken token)
        {
            var buffer = new byte[4096];

            try
            {
                while (_webSocket != null && _webSocket.State == WebSocketState.Open && !token.IsCancellationRequested)
                {
                    var result = await _webSocket.ReceiveAsync(new ArraySegment<byte>(buffer), token);

                    if (result.MessageType == WebSocketMessageType.Close)
                    {
                        await DisconnectAsync();
                        break;
                    }

                    if (result.Count > 0)
                    {
                        var data = new byte[result.Count];
                        Array.Copy(buffer, data, result.Count);

                        string message = Config.IsReceivedByHex
                            ? HexTool.ToHexStringFromDataBytes(data)
                            : Encoding.Default.GetString(data).Trim('\0');

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
            catch (OperationCanceledException)
            {
                // 正常取消
            }
            catch (Exception ex)
            {
                RaiseErrorOccurred(ex);
            }
        }

        protected override async Task<bool> SendCoreAsync(byte[] data)
        {
            try
            {
                if (_webSocket == null || _webSocket.State != WebSocketState.Open)
                    return false;

                await _webSocket.SendAsync(
                    new ArraySegment<byte>(data),
                    WebSocketMessageType.Binary,
                    true,
                    CancellationToken.None);
                return true;
            }
            catch
            {
                return false;
            }
        }

        protected override async Task<byte[]> ReceiveCoreAsync()
        {
            return await Task.FromResult<byte[]>(null);
        }

        public override void Dispose()
        {
            base.Dispose();
            _webSocket?.Dispose();
            _cts?.Dispose();
        }
    }
}