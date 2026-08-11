using System;
using System.Collections.Concurrent;
using System.IO;
using System.Threading.Tasks;

namespace LYCorePro.Common.Helper
{
    /// <summary>
    /// 日志级别
    /// </summary>
    public enum eMsgType
    {
        /// <summary>信息</summary>
        Info,
        /// <summary>警告</summary>
        Warning,
        /// <summary>错误</summary>
        Error,
        /// <summary>调试</summary>
        Debug,
        /// <summary>成功</summary>
        Success
    }

    /// <summary>
    /// 日志服务 - 提供统一的日志记录功能
    /// 支持控制台输出和文件写入
    /// </summary>
    public static class Logger
    {
        private static readonly ConcurrentQueue<string> _logQueue = new ConcurrentQueue<string>();
        private static bool _isRunning = false;
        private static readonly object _lock = new object();
        private static string _logFilePath = "Logs";

        static Logger()
        {
            StartLogging();
        }

        /// <summary>
        /// 设置日志文件路径
        /// </summary>
        public static void SetLogPath(string path)
        {
            _logFilePath = path;
            if (!Directory.Exists(_logFilePath))
            {
                Directory.CreateDirectory(_logFilePath);
            }
        }

        /// <summary>
        /// 添加日志
        /// </summary>
        /// <param name="message">日志消息</param>
        /// <param name="type">日志级别</param>
        /// <param name="isDispGrowl">是否显示 Growl 通知</param>
        public static void AddLog(string message, eMsgType type = eMsgType.Info, bool isDispGrowl = false)
        {
            var time = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff");
            var typeStr = type.ToString();
            var logEntry = $"[{time}] [{typeStr}] {message}";

            _logQueue.Enqueue(logEntry);

            // 控制台输出
            Console.WriteLine(logEntry);
        }

        /// <summary>
        /// 添加错误日志（含异常信息）
        /// </summary>
        public static void AddError(string message, Exception ex = null)
        {
            var fullMessage = message;
            if (ex != null)
            {
                fullMessage += $"\n异常: {ex.Message}\n堆栈: {ex.StackTrace}";
            }
            AddLog(fullMessage, eMsgType.Error);
        }

        /// <summary>
        /// 启动日志写入任务
        /// </summary>
        private static void StartLogging()
        {
            if (_isRunning) return;

            _isRunning = true;
            Task.Run(async () =>
            {
                while (_isRunning)
                {
                    try
                    {
                        while (_logQueue.TryDequeue(out var log))
                        {
                            WriteLogToFile(log);
                        }
                        await Task.Delay(100);
                    }
                    catch
                    {
                        // 忽略日志写入错误
                    }
                }
            });
        }

        /// <summary>
        /// 写入日志到文件
        /// </summary>
        private static void WriteLogToFile(string log)
        {
            try
            {
                if (!Directory.Exists(_logFilePath))
                {
                    Directory.CreateDirectory(_logFilePath);
                }

                var date = DateTime.Now.ToString("yyyy-MM-dd");
                var filePath = Path.Combine(_logFilePath, $"log_{date}.txt");

                lock (_lock)
                {
                    File.AppendAllText(filePath, log + Environment.NewLine);
                }
            }
            catch
            {
                // 忽略写入错误
            }
        }

        /// <summary>
        /// 停止日志服务
        /// </summary>
        public static void Stop()
        {
            _isRunning = false;
        }
    }
}