using LYCorePro.Common.Enum;
using System.IO.Ports;

namespace LYCorePro.Communacation
{
    /// <summary>
    /// 通讯配置类 - 存储所有通讯参数
    /// 用于 CommunicationFactory 创建通讯实例
    /// </summary>
    public class CommunicationConfig
    {
        #region 基础属性

        /// <summary>通讯唯一标识 Key</summary>
        public string Key { get; set; }

        /// <summary>通讯名称</summary>
        public string Name { get; set; }

        /// <summary>备注</summary>
        public string Remarks { get; set; }

        /// <summary>通讯类型</summary>
        public eCommunicationType CommunicationType { get; set; }

        /// <summary>是否为 PLC 协议</summary>
        public bool IsPLC { get; set; }

        #endregion

        #region 网络参数

        /// <summary>远程 IP 地址（默认 127.0.0.1）</summary>
        public string RemoteIP { get; set; } = "127.0.0.1";

        /// <summary>远程端口（默认 9000）</summary>
        public int RemotePort { get; set; } = 9000;

        /// <summary>本地端口（TCP Server / UDP 使用，默认 8000）</summary>
        public int LocalPort { get; set; } = 8000;

        #endregion

        #region 串口参数

        /// <summary>串口号（默认 COM1）</summary>
        public string PortName { get; set; } = "COM1";

        /// <summary>波特率（默认 9600）</summary>
        public int BaudRate { get; set; } = 9600;

        /// <summary>校验位</summary>
        public Parity Parity { get; set; } = Parity.None;

        /// <summary>数据位（默认 8）</summary>
        public int DataBits { get; set; } = 8;

        /// <summary>停止位</summary>
        public StopBits StopBits { get; set; } = StopBits.One;

        #endregion

        #region PLC 参数

        /// <summary>PLC 类型</summary>
        public PLCType PLCType { get; set; }

        /// <summary>Modbus PLC 子类型</summary>
        public ModbusPLCType ModbusPLCType { get; set; }

        /// <summary>站号（默认 1）</summary>
        public int StationCode { get; set; } = 1;

        /// <summary>地址是否从 0 开始（默认 true）</summary>
        public bool StartWithZero { get; set; } = true;

        /// <summary>数据格式（CDAB/ABCD/BADC/DCBA）</summary>
        public string DataFormat { get; set; } = "CDAB";

        /// <summary>槽号（西门子 S7 使用）</summary>
        public int Slot { get; set; } = 0;

        /// <summary>机架号（西门子 S7 使用）</summary>
        public int Rack { get; set; } = 0;

        #endregion

        #region OPC UA 参数

        /// <summary>OPC UA 服务器 IP</summary>
        public string OpcUaIP { get; set; } = "127.0.0.1";

        /// <summary>OPC UA 服务器端口（默认 4840）</summary>
        public int OpcUaPort { get; set; } = 4840;

        /// <summary>OPC UA 服务器 URL</summary>
        public string OpcUaServerUrl { get; set; } = "opc.tcp://localhost:4840";

        /// <summary>OPC UA 用户名（可选）</summary>
        public string OpcUaUserName { get; set; } = "";

        /// <summary>OPC UA 密码（可选）</summary>
        public string OpcUaPassword { get; set; } = "";

        #endregion

        #region 倍福 ADS 参数

        /// <summary>ADS AMS Net ID</summary>
        public string AdsAmsNetId { get; set; } = "127.0.0.1.1.1";

        /// <summary>ADS 端口（默认 851）</summary>
        public int AdsPort { get; set; } = 851;

        #endregion

        #region WebSocket / HTTP 参数

        /// <summary>WebSocket 服务器 IP</summary>
        public string WebSocketIP { get; set; } = "127.0.0.1";

        /// <summary>WebSocket 端口（默认 8080）</summary>
        public int WebSocketPort { get; set; } = 8080;

        /// <summary>WebSocket URL</summary>
        public string WebSocketUrl { get; set; } = "ws://localhost:8080/ws";

        /// <summary>HTTP 服务器 IP</summary>
        public string HttpBaseIP { get; set; } = "127.0.0.1";

        /// <summary>HTTP 端口（默认 8080）</summary>
        public int HttpBasePort { get; set; } = 8080;

        /// <summary>HTTP 基础 URL</summary>
        public string HttpBaseUrl { get; set; } = "http://localhost:8080";

        #endregion

        #region 超时和重试

        /// <summary>超时时间（毫秒，默认 5000）</summary>
        public int Timeout { get; set; } = 5000;

        /// <summary>重试次数（默认 3）</summary>
        public int RetryCount { get; set; } = 3;

        #endregion

        #region 发送接收参数

        /// <summary>是否以十六进制发送</summary>
        public bool IsSendByHex { get; set; }

        /// <summary>是否以十六进制接收</summary>
        public bool IsReceivedByHex { get; set; }

        #endregion
    }
}