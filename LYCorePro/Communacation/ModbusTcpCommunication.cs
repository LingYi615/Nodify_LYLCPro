using HslCommunication;
using HslCommunication.ModBus;
using System;
using System.Threading.Tasks;

namespace LYCorePro.Communacation
{
    /// <summary>
    /// Modbus TCP 通讯实现
    /// 使用 HslCommunication 库的 ModbusTcpNet 进行 Modbus TCP 协议通信
    /// 支持 TCP 连接断开检测
    /// </summary>
    public class ModbusTcpCommunication : CommunicationBase
    {
        private ModbusTcpNet _client;

        public override string ConnectionString => $"Modbus-TCP://{Config.RemoteIP}:{Config.RemotePort}";

        public ModbusTcpCommunication(CommunicationConfig config) : base(config) { }

        /// <summary>
        /// 检测 Modbus TCP 连接是否存活
        /// 通过反射访问 HslCommunication 内部 Socket 进行主动探测
        /// </summary>
        public override Task<bool> CheckConnectionAsync()
        {
            return Task.FromResult(IsHslSocketAlive(_client));
        }

        protected override async Task<bool> ConnectCoreAsync()
        {
            return await Task.Run(() =>
            {
                try
                {
                    _client = new ModbusTcpNet(Config.RemoteIP, Config.RemotePort, (byte)Config.StationCode);
                    _client.AddressStartWithZero = Config.StartWithZero;
                    _client.DataFormat = GetDataFormat(Config.DataFormat);
                    var result = _client.ConnectServer();
                    return result.IsSuccess;
                }
                catch (Exception ex)
                {
                    RaiseErrorOccurred(ex);
                    return false;
                }
            });
        }

        protected override async Task<bool> DisconnectCoreAsync()
        {
            return await Task.Run(() =>
            {
                try { _client?.ConnectClose(); return true; }
                catch { return false; }
            });
        }

        protected override async Task<bool> SendCoreAsync(byte[] data)
        {
            return await Task.Run(async () =>
            {
                try
                {
                    var result = _client.ReadFromCoreServer(data);
                    if (!result.IsSuccess)
                        await HandleOperationFailureAsync();
                    return result.IsSuccess;
                }
                catch
                {
                    await HandleOperationFailureAsync();
                    return false;
                }
            });
        }

        protected override async Task<byte[]> ReceiveCoreAsync()
        {
            return await Task.FromResult<byte[]>(null);
        }

        public override async Task<T> ReadAsync<T>(string address)
        {
            if (!IsConnected) return default;
            return await Task.Run(() =>
            {
                object result = null;
                switch (Type.GetTypeCode(typeof(T)))
                {
                    case TypeCode.Boolean: var r1 = _client.ReadBool(address); if (r1.IsSuccess) result = r1.Content; break;
                    case TypeCode.Int16: var r2 = _client.ReadInt16(address); if (r2.IsSuccess) result = r2.Content; break;
                    case TypeCode.UInt16: var r3 = _client.ReadUInt16(address); if (r3.IsSuccess) result = r3.Content; break;
                    case TypeCode.Int32: var r4 = _client.ReadInt32(address); if (r4.IsSuccess) result = r4.Content; break;
                    case TypeCode.UInt32: var r5 = _client.ReadUInt32(address); if (r5.IsSuccess) result = r5.Content; break;
                    case TypeCode.Single: var r6 = _client.ReadFloat(address); if (r6.IsSuccess) result = r6.Content; break;
                    case TypeCode.Double: var r7 = _client.ReadDouble(address); if (r7.IsSuccess) result = r7.Content; break;
                    default: throw new NotSupportedException($"不支持的类型: {typeof(T).Name}");
                }
                return result != null ? (T)result : default;
            });
        }

        public override async Task<bool> WriteAsync<T>(string address, T value)
        {
            if (!IsConnected) return false;
            return await Task.Run(() =>
            {
                OperateResult result = null;
                switch (Type.GetTypeCode(typeof(T)))
                {
                    case TypeCode.Boolean: result = _client.Write(address, (bool)(object)value); break;
                    case TypeCode.Int16: result = _client.Write(address, (short)(object)value); break;
                    case TypeCode.UInt16: result = _client.Write(address, (ushort)(object)value); break;
                    case TypeCode.Int32: result = _client.Write(address, (int)(object)value); break;
                    case TypeCode.UInt32: result = _client.Write(address, (uint)(object)value); break;
                    case TypeCode.Single: result = _client.Write(address, (float)(object)value); break;
                    case TypeCode.Double: result = _client.Write(address, (double)(object)value); break;
                    default: throw new NotSupportedException($"不支持的类型: {typeof(T).Name}");
                }
                return result?.IsSuccess ?? false;
            });
        }

        public override async Task<string> ReadStringAsync(string address, ushort length)
        {
            if (!IsConnected) return "";
            return await Task.Run(() =>
            {
                var result = _client.ReadString(address, length);
                return result.IsSuccess ? result.Content : "";
            });
        }

        public override async Task<bool> WriteStringAsync(string address, string value)
        {
            if (!IsConnected) return false;
            return await Task.Run(() =>
            {
                var result = _client.Write(address, value);
                return result.IsSuccess;
            });
        }

        private HslCommunication.Core.DataFormat GetDataFormat(string format)
        {
            switch (format.ToUpper())
            {
                case "ABCD": return HslCommunication.Core.DataFormat.ABCD;
                case "BADC": return HslCommunication.Core.DataFormat.BADC;
                case "CDAB": return HslCommunication.Core.DataFormat.CDAB;
                case "DCBA": return HslCommunication.Core.DataFormat.DCBA;
                default: return HslCommunication.Core.DataFormat.CDAB;
            }
        }

        public override void Dispose()
        {
            base.Dispose();
            _client?.Dispose();
        }
    }
}