using System;
using LYCorePro.Common.Enum;

namespace LYCorePro.Communacation
{
    /// <summary>
    /// 通讯工厂 - 根据通讯配置创建对应的通讯实例
    /// 支持 16 种通讯协议：TCP/UDP/串口/Modbus/S7/FINS/CIP/ADS/OPC UA/WebSocket/HTTPClient/HTTPServer
    /// </summary>
    public static class CommunicationFactory
    {
        /// <summary>
        /// 根据配置创建通讯实例
        /// </summary>
        /// <param name="config">通讯配置</param>
        /// <returns>对应协议的通讯实例</returns>
        /// <exception cref="ArgumentNullException">配置为空时抛出</exception>
        /// <exception cref="NotSupportedException">不支持的通讯类型时抛出</exception>
        public static ICommunication Create(CommunicationConfig config)
        {
            if (config == null)
                throw new ArgumentNullException(nameof(config));

            switch (config.CommunicationType)
            {
                case eCommunicationType.TCPClient:
                    return new TcpClientCommunication(config);
                case eCommunicationType.TCPServer:
                    return new TcpServerCommunication(config);
                case eCommunicationType.UDP:
                    return new UdpCommunication(config);
                case eCommunicationType.Serial:
                    return new SerialCommunication(config);
                case eCommunicationType.ModbusRTU:
                    return new ModbusRtuCommunication(config);
                case eCommunicationType.ModbusTCP:
                    return new ModbusTcpCommunication(config);
                case eCommunicationType.ModbusASCII:
                    return new ModbusAsciiCommunication(config);
                case eCommunicationType.SiemensS7:
                    return new SiemensS7Communication(config);
                case eCommunicationType.OmronFinsTCP:
                    return new OmronFinsTcpCommunication(config);
                case eCommunicationType.OmronFinsUDP:
                    return new OmronFinsUdpCommunication(config);
                case eCommunicationType.AllenBradleyCIP:
                    return new AllenBradleyCipCommunication(config);
                case eCommunicationType.BeckhoffADS:
                    return new BeckhoffAdsCommunication(config);
                case eCommunicationType.OpcUA:
                    return new OpcUaCommunication(config);
                case eCommunicationType.WebSocket:
                    return new WebSocketCommunication(config);
                case eCommunicationType.HTTPClient:
                    return new HttpClientCommunication(config);
                case eCommunicationType.HTTPServer:
                    return new HttpServerCommunication(config);
                default:
                    throw new NotSupportedException($"不支持的通讯类型: {config.CommunicationType}");
            }
        }

        /// <summary>
        /// 获取通讯类型的中文显示名称
        /// </summary>
        public static string GetDisplayName(eCommunicationType type)
        {
            return type.GetDisplayName();
        }

        /// <summary>
        /// 获取通讯类型需要的参数列表
        /// </summary>
        public static System.Collections.Generic.List<string> GetRequiredParameters(eCommunicationType type)
        {
            var parameters = new System.Collections.Generic.List<string>();

            switch (type)
            {
                case eCommunicationType.TCPClient:
                case eCommunicationType.TCPServer:
                case eCommunicationType.UDP:
                case eCommunicationType.ModbusTCP:
                case eCommunicationType.SiemensS7:
                case eCommunicationType.OmronFinsTCP:
                case eCommunicationType.OmronFinsUDP:
                case eCommunicationType.AllenBradleyCIP:
                case eCommunicationType.BeckhoffADS:
                    parameters.Add("IP 地址");
                    parameters.Add("端口号");
                    break;

                case eCommunicationType.Serial:
                case eCommunicationType.ModbusRTU:
                case eCommunicationType.ModbusASCII:
                    parameters.Add("串口号");
                    parameters.Add("波特率");
                    break;

                case eCommunicationType.OpcUA:
                    parameters.Add("服务器地址");
                    break;

                case eCommunicationType.WebSocket:
                    parameters.Add("WebSocket 地址");
                    break;

                case eCommunicationType.HTTPClient:
                    parameters.Add("基础 URL");
                    break;
                case eCommunicationType.HTTPServer:
                    parameters.Add("监听 IP");
                    parameters.Add("监听端口");
                    break;

                default:
                    break;
            }

            return parameters;
        }
    }
}
