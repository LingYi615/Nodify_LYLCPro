using System.Collections.Generic;
using LYCorePro.Common.Enum;

namespace TcpsCommPlugin
{
    /// <summary>
    /// 通讯协议配置（外部配置好的通讯协议，用于下拉选择快速填充参数）
    /// </summary>
    public class ProtocolConfig
    {
        /// <summary>协议名称（用于下拉选择显示）</summary>
        public string Name { get; set; } = string.Empty;

        /// <summary>主机地址</summary>
        public string Host { get; set; } = "127.0.0.1";

        /// <summary>端口</summary>
        public int Port { get; set; } = 502;

        /// <summary>通讯类型</summary>
        public eCommunicationType CommunicationType { get; set; } = eCommunicationType.TCPClient;

        /// <summary>通讯类型显示名称</summary>
        public string ProtocolTypeDisplay => CommunicationType.GetDisplayName();

        /// <summary>超时时间（毫秒）</summary>
        public int Timeout { get; set; } = 3000;

        /// <summary>
        /// 预置的外部通讯协议列表（静态属性，供 XAML 绑定）
        /// </summary>
        public static List<ProtocolConfig> PresetProtocols { get; } = new()
        {
            new ProtocolConfig
            {
                Name = "Modbus TCP (localhost)",
                Host = "127.0.0.1", Port = 502,
                CommunicationType = eCommunicationType.ModbusTCP,
                Timeout = 3000
            },
            new ProtocolConfig
            {
                Name = "SiemensS7 PLC",
                Host = "192.168.0.1", Port = 102,
                CommunicationType = eCommunicationType.SiemensS7,
                Timeout = 5000
            },
            new ProtocolConfig
            {
                Name = "Mitsubishi MC Protocol",
                Host = "192.168.1.1", Port = 5000,
                CommunicationType = eCommunicationType.TCPClient,
                Timeout = 3000
            },
            new ProtocolConfig
            {
                Name = "自定义 TCP 设备",
                Host = "127.0.0.1", Port = 8080,
                CommunicationType = eCommunicationType.TCPClient,
                Timeout = 5000
            },
            new ProtocolConfig
            {
                Name = "TCP 服务器（本地监听）",
                Host = "0.0.0.0", Port = 8000,
                CommunicationType = eCommunicationType.TCPServer,
                Timeout = 5000
            },
            new ProtocolConfig
            {
                Name = "UDP 通讯",
                Host = "127.0.0.1", Port = 9000,
                CommunicationType = eCommunicationType.UDP,
                Timeout = 3000
            },
            new ProtocolConfig
            {
                Name = "OPC UA Server",
                Host = "localhost", Port = 4840,
                CommunicationType = eCommunicationType.OpcUA,
                Timeout = 5000
            },
        };
    }
}