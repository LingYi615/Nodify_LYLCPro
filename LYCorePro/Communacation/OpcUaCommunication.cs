using Opc.Ua;
using OpcUaHelper;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace LYCorePro.Communacation
{
    /// <summary>
    /// OPC UA 通讯实现
    /// 支持 TCP 连接断开检测
    /// </summary>
    public class OpcUaCommunication : CommunicationBase
    {
        private OpcUaClient _client;
        private bool _isConnected = false;

        public override string ConnectionString => $"OPC-UA://{Config.OpcUaServerUrl}";

        public OpcUaCommunication(CommunicationConfig config) : base(config)
        {
        }

        /// <summary>
        /// 检测 OPC UA 连接是否存活
        /// 通过实际读取 ServerStatus 节点来验证连接，比 _client.Connected 更可靠
        /// </summary>
        public override async Task<bool> CheckConnectionAsync()
        {
            try
            {
                if (_client == null) return false;
                if (!_client.Connected) return false;

                // 通过实际读取 OPC UA 标准节点来验证连接
                // ServerStatus 是每个 OPC UA 服务器都有的标准节点
                await Task.Run(() =>
                {
                    _client.ReadNode("i=2256"); // ServerStatus 节点
                });
                return true;
            }
            catch
            {
                return false;
            }
        }

        protected override async Task<bool> ConnectCoreAsync()
        {
            try
            {
                _client = new OpcUaClient();

                if (!string.IsNullOrEmpty(Config.OpcUaUserName))
                {
                    // 设置用户名密码认证
                    // _client.SetCredentials(Config.OpcUaUserName, Config.OpcUaPassword);
                }

                await _client.ConnectServer(Config.OpcUaServerUrl);
                _isConnected = _client.Connected;
                return _isConnected;
            }
            catch (Exception ex)
            {
                RaiseErrorOccurred(ex);
                return false;
            }
        }

        protected override async Task<bool> DisconnectCoreAsync()
        {
            return await Task.Run(() =>
            {
                try
                {
                    _client?.Disconnect();
                    _isConnected = false;
                    return true;
                }
                catch
                {
                    return false;
                }
            });
        }

        protected override async Task<bool> SendCoreAsync(byte[] data)
        {
            return await Task.FromResult(false);
        }


        protected override async Task<byte[]> ReceiveCoreAsync()
        {
            return await Task.FromResult<byte[]>(null);
        }

        public override async Task<T> ReadAsync<T>(string address)
        {
            if (!IsConnected) return default;

            try
            {
                return await Task.Run(() =>
                {
                    var result = _client.ReadNode(address);
                    if (result == null) return default;

                    var value = result.Value;
                    if (value is T tValue)
                        return tValue;

                    try
                    {
                        return (T)Convert.ChangeType(value, typeof(T));
                    }
                    catch
                    {
                        return default;
                    }
                });
            }
            catch
            {
                // 读取失败，检测连接状态
                await HandleOperationFailureAsync();
                return default;
            }
        }

        public override async Task<bool> WriteAsync<T>(string address, T value)
        {
            if (!IsConnected) return false;

            try
            {
                return await Task.Run(() =>
                {
                    _client.WriteNode(address, value);
                    return true;
                });
            }
            catch
            {
                await HandleOperationFailureAsync();
                return false;
            }
        }

        public override async Task<string> ReadStringAsync(string address, ushort length)
        {
            if (!IsConnected) return "";

            try
            {
                return await Task.Run(() =>
                {
                    return _client.ReadNode<string>(address);
                });
            }
            catch
            {
                await HandleOperationFailureAsync();
                return "";
            }
        }

        public override async Task<bool> WriteStringAsync(string address, string value)
        {
            if (!IsConnected) return false;

            try
            {
                return await Task.Run(() =>
                {
                    _client.WriteNode(address, value);
                    return true;
                });
            }
            catch
            {
                await HandleOperationFailureAsync();
                return false;
            }
        }

        /// <summary>
        /// OPC UA 专用：订阅节点（兼容 OpcUaHelper 2.0.0 版本）
        /// </summary>
        public void SubscribeNode(string nodeId, Action<object> callback)
        {
            if (!IsConnected || _client == null)
                throw new InvalidOperationException("未连接");

            try
            {
                // 2.0.0 版本的正确签名：AddSubscription(string key, string nodeId, Action<string, MonitoredItem, MonitoredItemNotificationEventArgs> callback)
                string subscriptionKey = nodeId; // 使用 nodeId 作为订阅键
                _client.AddSubscription(subscriptionKey, nodeId, (key, monitoredItem, args) =>
                {
                    try
                    {
                        // 从事件参数中提取值
                        var notification = args.NotificationValue as MonitoredItemNotification;
                        if (notification != null)
                        {
                            // Variant 类型直接访问 Value 属性，不使用 ?.
                            var value = notification.Value?.Value;
                            callback?.Invoke(value);
                        }
                    }
                    catch (Exception ex)
                    {
                        // 处理回调中的错误
                        RaiseErrorOccurred(new Exception($"订阅回调错误: {ex.Message}"));
                    }
                });
            }
            catch (Exception ex)
            {
                throw new Exception($"订阅节点失败: {ex.Message}");
            }
        }

        #region 订阅逻辑示例
        // 关键：定义当温度变化时的处理逻辑
        //_opcClient.SubscribeNode("ns=2;s=Temperature", OnTemperatureChanged);

        // 实际的业务处理方法，它会被回调函数调用
        //private void OnTemperatureChanged(object newValue)
        //{
        //    // 在这里处理获取到的值！
        //    // 1. 更新UI
        //    // 2. 触发报警
        //    // 3. 写入数据库
        //    // 4. 执行设备控制
        //    Console.WriteLine($"温度更新为: {newValue}°C");
        //
        //    if (Convert.ToDouble(newValue) > 100.0)
        //    {
        //        Console.WriteLine("!!!!! 高温警报 !!!!!");
        //        // 触发你的报警逻辑...
        //    }
        //}
        #endregion

        /// <summary>
        /// OPC UA 专用：批量读取节点
        /// </summary>
        public async Task<Dictionary<string, object>> ReadNodesAsync(params string[] nodeIds)
        {
            if (!IsConnected || _client == null)
                throw new InvalidOperationException("未连接");

            return await Task.Run(() =>
            {
                lock (_syncLock)
                {
                    var result = new Dictionary<string, object>();

                    foreach (var nodeId in nodeIds)
                    {
                        try
                        {
                            var dataValue = _client.ReadNode(nodeId);
                            result[nodeId] = dataValue?.Value;
                        }
                        catch (Exception ex)
                        {
                            result[nodeId] = $"错误: {ex.Message}";
                        }
                    }

                    return result;
                }
            });
        }

        public override void Dispose()
        {
            base.Dispose();
            _client?.Disconnect();
            //_client?.Dispose();
        }
    }
}