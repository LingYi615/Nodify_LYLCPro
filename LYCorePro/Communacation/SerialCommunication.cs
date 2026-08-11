using System;
using System.IO.Ports;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace LYCorePro.Communacation
{
    /// <summary>
    /// 串口通讯实现
    /// 使用 SerialPort 进行串口数据收发
    /// </summary>
    public class SerialCommunication : CommunicationBase
    {
        private SerialPort _serialPort;
        private CancellationTokenSource _cts;

        public override string ConnectionString => $"Serial://{Config.PortName}:{Config.BaudRate}";

        public SerialCommunication(CommunicationConfig config) : base(config) { }

        protected override async Task<bool> ConnectCoreAsync()
        {
            try
            {
                _serialPort = new SerialPort(Config.PortName, Config.BaudRate, Config.Parity, Config.DataBits, Config.StopBits);
                _serialPort.ReadTimeout = Config.Timeout;
                _serialPort.WriteTimeout = Config.Timeout;

                await Task.Run(() => _serialPort.Open());
                _cts = new CancellationTokenSource();
                _ = ReceiveLoopAsync(_cts.Token);
                return _serialPort.IsOpen;
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
                await Task.Run(() =>
                {
                    if (_serialPort != null && _serialPort.IsOpen)
                        _serialPort.Close();
                    _serialPort?.Dispose();
                });
                return true;
            }
            catch { return false; }
        }

        private async Task ReceiveLoopAsync(CancellationToken token)
        {
            try
            {
                while (!token.IsCancellationRequested && _serialPort != null && _serialPort.IsOpen)
                {
                    var bytesToRead = _serialPort.BytesToRead;
                    if (bytesToRead > 0)
                    {
                        var data = new byte[bytesToRead];
                        await Task.Run(() => _serialPort.Read(data, 0, bytesToRead));
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
                    else
                    {
                        await Task.Delay(10, token);
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
                await Task.Run(() =>
                {
                    if (_serialPort != null && _serialPort.IsOpen)
                        _serialPort.Write(data, 0, data.Length);
                });
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
            _serialPort?.Close();
            _serialPort?.Dispose();
        }
    }
}