using System;

namespace LYCorePro.Common.Enum
{
    /// <summary>
    /// 枚举描述特性 - 用于为枚举值提供中文描述
    /// </summary>
    [AttributeUsage(AttributeTargets.Field, AllowMultiple = false)]
    public sealed class EnumDescriptionAttribute : Attribute
    {
        public string Description { get; }
        public EnumDescriptionAttribute(string description) : base()
        {
            this.Description = description;
        }
    }

    /// <summary>
    /// 通讯类型枚举 - 定义所有支持的通讯协议类型
    /// </summary>
    [Serializable]
    public enum eCommunicationType
    {
        /// <summary>TCP 客户端</summary>
        TCPClient = 0,
        /// <summary>TCP 服务器</summary>
        TCPServer = 1,
        /// <summary>UDP 通讯</summary>
        UDP = 2,
        /// <summary>串口通讯</summary>
        Serial = 3,
        /// <summary>Modbus RTU</summary>
        ModbusRTU = 4,
        /// <summary>Modbus TCP</summary>
        ModbusTCP = 5,
        /// <summary>Modbus ASCII</summary>
        ModbusASCII = 6,
        /// <summary>西门子 S7</summary>
        SiemensS7 = 7,
        /// <summary>欧姆龙 FINS TCP</summary>
        OmronFinsTCP = 8,
        /// <summary>欧姆龙 FINS UDP</summary>
        OmronFinsUDP = 9,
        /// <summary>罗克韦尔 CIP (EtherNet/IP)</summary>
        AllenBradleyCIP = 10,
        /// <summary>倍福 ADS</summary>
        BeckhoffADS = 11,
        /// <summary>OPC UA</summary>
        OpcUA = 12,
        /// <summary>WebSocket</summary>
        WebSocket = 13,
        /// <summary>HTTPClient</summary>
        HTTPClient = 14,
        /// <summary>HTTPServer</summary>
        HTTPServer = 15
    }

    /// <summary>
    /// 通讯类型扩展方法
    /// </summary>
    public static class CommunicationTypeExtensions
    {
        /// <summary>
        /// 获取通讯类型的中文显示名称
        /// </summary>
        public static string GetDisplayName(this eCommunicationType type)
        {
            switch (type)
            {
                case eCommunicationType.TCPClient: return "TCP 客户端";
                case eCommunicationType.TCPServer: return "TCP 服务器";
                case eCommunicationType.UDP: return "UDP 通讯";
                case eCommunicationType.Serial: return "串口通讯";
                case eCommunicationType.ModbusRTU: return "Modbus RTU";
                case eCommunicationType.ModbusTCP: return "Modbus TCP";
                case eCommunicationType.ModbusASCII: return "Modbus ASCII";
                case eCommunicationType.SiemensS7: return "西门子 S7";
                case eCommunicationType.OmronFinsTCP: return "欧姆龙 FINS TCP";
                case eCommunicationType.OmronFinsUDP: return "欧姆龙 FINS UDP";
                case eCommunicationType.AllenBradleyCIP: return "罗克韦尔 CIP";
                case eCommunicationType.BeckhoffADS: return "倍福 ADS";
                case eCommunicationType.OpcUA: return "OPC UA";
                case eCommunicationType.WebSocket: return "WebSocket";
                case eCommunicationType.HTTPClient: return "HTTPClient";
                case eCommunicationType.HTTPServer: return "HTTPServer";
                default: return type.ToString();
            }
        }

        /// <summary>
        /// 判断是否为 PLC 协议（支持 Read/Write 变量操作）
        /// </summary>
        public static bool IsPLC(this eCommunicationType type)
        {
            switch (type)
            {
                case eCommunicationType.ModbusRTU:
                case eCommunicationType.ModbusTCP:
                case eCommunicationType.ModbusASCII:
                case eCommunicationType.SiemensS7:
                case eCommunicationType.OmronFinsTCP:
                case eCommunicationType.OmronFinsUDP:
                case eCommunicationType.AllenBradleyCIP:
                case eCommunicationType.BeckhoffADS:
                case eCommunicationType.OpcUA:
                    return true;
                default:
                    return false;
            }
        }

        /// <summary>
        /// 判断是否为串口协议
        /// </summary>
        public static bool IsSerial(this eCommunicationType type)
        {
            switch (type)
            {
                case eCommunicationType.Serial:
                case eCommunicationType.ModbusRTU:
                case eCommunicationType.ModbusASCII:
                    return true;
                default:
                    return false;
            }
        }

        /// <summary>
        /// 判断是否为网络协议
        /// </summary>
        public static bool IsNetwork(this eCommunicationType type)
        {
            return !type.IsSerial();
        }
    }

    /// <summary>
    /// PLC 类型枚举
    /// </summary>
    [Serializable]
    public enum PLCType
    {
        /// <summary>无</summary>
        None = 0,
        /// <summary>倍福 ADS</summary>
        Ads = 1,
        /// <summary>OPC UA</summary>
        OpcUa = 2,
        /// <summary>罗克韦尔 CIP</summary>
        Cipnet = 3,
        /// <summary>欧姆龙 FINS TCP</summary>
        FinsTcp = 4,
        /// <summary>欧姆龙 FINS UDP</summary>
        FinsUdp = 5,
        /// <summary>西门子 S7</summary>
        Siemens = 6,
        /// <summary>Modbus</summary>
        Modbus = 7
    }

    /// <summary>
    /// PLC 类型扩展方法
    /// </summary>
    public static class PLCTypeExtensions
    {
        /// <summary>
        /// 获取 PLC 类型的中文显示名称
        /// </summary>
        public static string GetDisplayName(this PLCType type)
        {
            switch (type)
            {
                case PLCType.None: return "无";
                case PLCType.Ads: return "倍福 ADS";
                case PLCType.OpcUa: return "OPC UA";
                case PLCType.Cipnet: return "罗克韦尔 CIP";
                case PLCType.FinsTcp: return "欧姆龙 FINS TCP";
                case PLCType.FinsUdp: return "欧姆龙 FINS UDP";
                case PLCType.Siemens: return "西门子 S7";
                case PLCType.Modbus: return "Modbus";
                default: return type.ToString();
            }
        }
    }

    /// <summary>
    /// Modbus PLC 子类型枚举
    /// </summary>
    [Serializable]
    public enum ModbusPLCType
    {
        /// <summary>Modbus RTU</summary>
        ModbusRTU = 0,
        /// <summary>Modbus TCP</summary>
        ModbusTCP = 1,
        /// <summary>Modbus ASCII</summary>
        ModbusASCII = 2
    }

    /// <summary>
    /// Modbus PLC 类型扩展方法
    /// </summary>
    public static class ModbusPLCTypeExtensions
    {
        /// <summary>
        /// 获取 Modbus 类型的中文显示名称
        /// </summary>
        public static string GetDisplayName(this ModbusPLCType type)
        {
            switch (type)
            {
                case ModbusPLCType.ModbusRTU: return "Modbus RTU";
                case ModbusPLCType.ModbusTCP: return "Modbus TCP";
                case ModbusPLCType.ModbusASCII: return "Modbus ASCII";
                default: return type.ToString();
            }
        }
    }

    /// <summary>
    /// 流程状态
    /// </summary>
    [Serializable]
    public enum eRunMode
    {
        None = 0,
        /// <summary>
        /// 运行一次
        /// </summary>
        RunOnce = 1,
        /// <summary>
        /// 循环运行
        /// </summary>
        RunCycle = 2,
    }

    public enum eRunStatus
    {
        /// <summary>
        /// 运行成功
        /// </summary>
        OK = 0,
        /// <summary>
        /// 运行失败
        /// </summary>
        NG = 1,
        /// <summary>
        /// 未运行
        /// </summary>
        NotRun = 2,
        /// <summary>
        /// 运行中
        /// </summary>
        Running = 3,
    }

}
