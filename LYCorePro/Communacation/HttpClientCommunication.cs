using LYCorePro.Common.Helper;
using System;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace LYCorePro.Communacation
{
    /// <summary>
    /// HTTP 通讯实现 - 请求响应模式
    /// </summary>
    public class HttpClientCommunication : CommunicationBase
    {
        private HttpClient _httpClient;
        private bool _isReceiving;
        private readonly object _lock = new object();

        public override string ConnectionString => $"HTTP://{Config.HttpBaseUrl}";

        public HttpClientCommunication(CommunicationConfig config) : base(config)
        {
        }

        protected override async Task<bool> ConnectCoreAsync()
        {
            try
            {
                _httpClient = new HttpClient();
                _httpClient.BaseAddress = new Uri(Config.HttpBaseUrl);
                _httpClient.Timeout = TimeSpan.FromMilliseconds(Config.Timeout);

                // 测试连接
                var response = await _httpClient.GetAsync("/");
                return response.IsSuccessStatusCode;
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
                _isReceiving = false;
                _httpClient?.CancelPendingRequests();
                _httpClient?.Dispose();
                _httpClient = null;
                return true;
            }
            catch
            {
                return false;
            }
        }

        /// <summary>
        /// 发送数据并接收响应（请求-响应模式）
        /// </summary>
        protected override async Task<bool> SendCoreAsync(byte[] data)
        {
            try
            {
                var content = new ByteArrayContent(data);
                content.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue("application/octet-stream");

                using (var cts = new CancellationTokenSource(TimeSpan.FromMilliseconds(Config.Timeout)))
                {
                    // 发送请求
                    var response = await _httpClient.PostAsync("/api/data", content, cts.Token);

                    if (response.IsSuccessStatusCode)
                    {
                        // 读取响应数据
                        var responseData = await response.Content.ReadAsByteArrayAsync();

                        // 如果有响应数据，触发接收事件
                        if (responseData != null && responseData.Length > 0)
                        {
                            // 使用基类的 OnDataReceived 方法
                            OnDataReceived(responseData);
                        }

                        return true;
                    }
                    else
                    {
                        var errorContent = await response.Content.ReadAsStringAsync();
                        Logger.AddLog($"HTTP请求失败: {response.StatusCode} - {errorContent}", eMsgType.Error);
                        return false;
                    }
                }
            }
            catch (TaskCanceledException)
            {
                Logger.AddLog($"HTTP请求超时: {Config.Timeout}ms", eMsgType.Error);
                return false;
            }
            catch (Exception ex)
            {
                Logger.AddLog($"HTTP发送失败: {ex.Message}", eMsgType.Error);
                RaiseErrorOccurred(ex);
                return false;
            }
        }

        /// <summary>
        /// 接收核心方法 - 仅在需要主动接收时调用
        /// </summary>
        protected override async Task<byte[]> ReceiveCoreAsync()
        {
            return await Task.FromResult<byte[]>(null);
        }

        /// <summary>
        /// 主动 GET 请求接收数据
        /// </summary>
        public async Task<byte[]> ReceiveFromGetAsync(string endpoint = "/api/receive")
        {
            if (_httpClient == null) return null;

            try
            {
                using (var cts = new CancellationTokenSource(TimeSpan.FromMilliseconds(Config.Timeout)))
                {
                    var response = await _httpClient.GetAsync(endpoint, cts.Token);

                    if (response.IsSuccessStatusCode)
                    {
                        var data = await response.Content.ReadAsByteArrayAsync();
                        if (data != null && data.Length > 0)
                        {
                            // 使用基类的 OnDataReceived 方法
                            OnDataReceived(data);
                        }
                        return data;
                    }
                    return null;
                }
            }
            catch (TaskCanceledException)
            {
                // 超时正常返回 null，不触发错误
                return null;
            }
            catch (Exception ex)
            {
                RaiseErrorOccurred(ex);
                return null;
            }
        }

        /// <summary>
        /// GET 请求（返回字符串）
        /// </summary>
        public async Task<string> GetAsync(string endpoint)
        {
            if (!IsConnected || _httpClient == null) return "";

            try
            {
                var response = await _httpClient.GetAsync(endpoint);
                response.EnsureSuccessStatusCode();
                var result = await response.Content.ReadAsStringAsync();

                // 触发消息接收事件
                RaiseMessageReceived(result);
                return result;
            }
            catch (Exception ex)
            {
                RaiseErrorOccurred(ex);
                return "";
            }
        }

        /// <summary>
        /// POST 请求（JSON）
        /// </summary>
        public async Task<string> PostJsonAsync(string endpoint, string json)
        {
            if (!IsConnected || _httpClient == null) return "";

            try
            {
                var content = new StringContent(json, Encoding.UTF8, "application/json");
                var response = await _httpClient.PostAsync(endpoint, content);
                response.EnsureSuccessStatusCode();
                var result = await response.Content.ReadAsStringAsync();

                // 触发消息接收事件
                RaiseMessageReceived(result);
                return result;
            }
            catch (Exception ex)
            {
                RaiseErrorOccurred(ex);
                return "";
            }
        }

        /// <summary>
        /// HTTP 泛型读取：使用 address 作为 API 端点发起 GET 请求，并将响应解析为目标类型
        /// </summary>
        public override async Task<T> ReadAsync<T>(string address)
        {
            var response = await GetAsync(address);
            if (string.IsNullOrEmpty(response))
                return default;

            try
            {
                return (T)Convert.ChangeType(response, typeof(T));
            }
            catch
            {
                return default;
            }
        }

        /// <summary>
        /// HTTP 字符串读取：使用 address 作为 API 端点发起 GET 请求
        /// </summary>
        public override async Task<string> ReadStringAsync(string address, ushort length)
        {
            return await GetAsync(address);
        }

        public override void Dispose()
        {
            base.Dispose();
            _isReceiving = false;
            _httpClient?.Dispose();
        }
    }
}
