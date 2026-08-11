using System;
using System.Net;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace LYCorePro.Communacation
{
    /// <summary>
    /// HTTP 服务器通讯实现
    /// 使用 HttpListener 提供 HTTP 服务器功能，支持 GET/POST 请求处理
    /// 可用于接收外部系统的 HTTP 回调、Webhook 等场景
    /// </summary>
    public class HttpServerCommunication : CommunicationBase
    {
        private HttpListener _listener;
        private CancellationTokenSource _cts;
        private bool _isRunning = false;

        /// <summary>HTTP 请求到达事件，参数为 (请求路径, 请求体, 响应回调)</summary>
        public event EventHandler<HttpRequestEventArgs> OnRequestReceived;

        public override string ConnectionString => $"HTTP-Server://{Config.HttpBaseIP}:{Config.HttpBasePort}";

        public HttpServerCommunication(CommunicationConfig config) : base(config) { }

        protected override async Task<bool> ConnectCoreAsync()
        {
            await DisconnectCoreAsync();
            try
            {
                _listener = new HttpListener();
                _listener.Prefixes.Add($"http://localhost:{Config.HttpBasePort}/");
                _listener.Prefixes.Add($"http://127.0.0.1:{Config.HttpBasePort}/");
                try
                {
                    //_listener.Prefixes.Add($"http://{Config.HttpBaseIP}:{Config.HttpBasePort}/");
                    //尝试添加外部访问前缀
                    //_listener.Prefixes.Add($"http://+:{Config.HttpBasePort}/");
                }
                catch (HttpListenerException)
                {
                    // 如需外部访问，请以管理员身份运行或执行：
                    //netsh http add urlacl url = http://+:{Config.HttpBasePort}/ user=Everyone
                }

                _listener.Start();
                _isRunning = true;
                _cts = new CancellationTokenSource();
                _ = ListenLoopAsync(_cts.Token);
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
                _isRunning = false;
                _cts?.Cancel();
                _cts?.Dispose();
                _cts = null;

                if (_listener != null)
                {
                    if (_listener.IsListening)
                    {
                        _listener.Stop();
                    }
                    _listener.Close();
                    _listener = null;
                }
                await Task.CompletedTask;
                return true;
            }
            catch
            {

                return false;
            }
        }

        /// <summary>
        /// 检测 HTTP 服务器是否运行中
        /// </summary>
        public override Task<bool> CheckConnectionAsync()
        {
            return Task.FromResult(_isRunning && _listener != null && _listener.IsListening);
        }

        private async Task ListenLoopAsync(CancellationToken token)
        {
            while (_isRunning && !token.IsCancellationRequested && _listener != null && _listener.IsListening)
            {
                try
                {
                    var context = await _listener.GetContextAsync();
                    _ = HandleRequestAsync(context);
                }
                catch (OperationCanceledException) { break; }
                catch (HttpListenerException) { break; }
                catch (ObjectDisposedException) { break; }
                catch (Exception ex)
                {
                    RaiseErrorOccurred(ex);
                    await Task.Delay(100, token);
                }
            }
        }

        private async Task HandleRequestAsync(HttpListenerContext context)
        {
            try
            {
                var request = context.Request;
                var response = context.Response;

                // 读取请求体
                string requestBody = "";
                if (request.HasEntityBody)
                {
                    using (var reader = new System.IO.StreamReader(request.InputStream, request.ContentEncoding))
                    {
                        requestBody = await reader.ReadToEndAsync();
                    }
                }

                string path = request.Url?.AbsolutePath ?? "/";
                string method = request.HttpMethod;

                // 将请求数据放入接收队列
                string message = $"[{method}] {path}";
                if (!string.IsNullOrEmpty(requestBody))
                    message += $"\n{requestBody}";

                lock (_syncLock)
                {
                    _receiveQueue.Enqueue(message);
                    RaiseMessageReceived(message + "              " + DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"));
                    _receiveSignal.Set();
                }

                // 触发请求事件
                var eventArgs = new HttpRequestEventArgs
                {
                    Path = path,
                    Method = method,
                    Body = requestBody,
                    QueryString = request.QueryString,
                    Response = response
                };
                OnRequestReceived?.Invoke(this, eventArgs);

                // 默认响应
                if (!eventArgs.Handled)
                {
                    response.StatusCode = 200;
                    response.ContentType = "application/json";
                    var responseBytes = Encoding.UTF8.GetBytes("{\"status\":\"ok\"}");
                    await response.OutputStream.WriteAsync(responseBytes, 0, responseBytes.Length);
                }

                response.Close();
            }
            catch (Exception ex)
            {
                RaiseErrorOccurred(ex);
            }
        }

        protected override async Task<bool> SendCoreAsync(byte[] data)
        {
            // HTTP 服务器模式不支持主动发送，由客户端请求驱动
            return await Task.FromResult(false);
        }

        protected override async Task<byte[]> ReceiveCoreAsync()
        {
            return await Task.FromResult<byte[]>(null);
        }

        /// <summary>
        /// 向指定路径注册自定义响应处理器
        /// 注意：handler 需要自行处理 context
        /// </summary>
        public void RegisterHandler(string path, Func<HttpListenerContext, Task> handler)
        {
            OnRequestReceived += async (s, e) =>
            {
                if (e.Path == path && !e.Handled)
                {
                    e.Handled = true;
                    //此处处理context
                }
            };
        }

        public override void Dispose()
        {
            base.Dispose();
            _cts?.Dispose();
            _listener?.Close();
        }
    }

    /// <summary>
    /// HTTP 请求事件参数
    /// </summary>
    public class HttpRequestEventArgs : EventArgs
    {
        /// <summary>请求路径</summary>
        public string Path { get; set; }

        /// <summary>请求方法 (GET/POST/PUT/DELETE)</summary>
        public string Method { get; set; }

        /// <summary>请求体内容</summary>
        public string Body { get; set; }

        /// <summary>查询字符串</summary>
        public System.Collections.Specialized.NameValueCollection QueryString { get; set; }

        /// <summary>HTTP 响应对象</summary>
        public HttpListenerResponse Response { get; set; }

        /// <summary>是否已处理（设为 true 阻止默认响应）</summary>
        public bool Handled { get; set; }
    }
}