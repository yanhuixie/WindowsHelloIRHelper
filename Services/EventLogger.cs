using System;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;
using WindowsHelloIRHelper.Constants;
using WindowsHelloIRHelper.Interfaces;

namespace WindowsHelloIRHelper.Services
{
    /// <summary>
    /// 事件日志记录器实现类
    /// 支持Windows事件日志和备用文件日志
    /// </summary>
    public class EventLogger : IEventLogger
    {
        private const string EventSourceName = "AutoCameraControl";
        private const string EventLogName = "Application";
        private readonly string _fallbackLogPath;
        private bool _eventLogAvailable;
        private readonly object _lockObject = new object();

        /// <summary>
        /// 初始化EventLogger实例
        /// </summary>
        public EventLogger()
        {
            // 设置备用日志文件路径
            var logDirectory = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                "AutoCameraControl",
                "Logs"
            );
            
            Directory.CreateDirectory(logDirectory);
            _fallbackLogPath = Path.Combine(logDirectory, $"AutoCameraControl_{DateTime.Now:yyyyMMdd}.log");
        }

        /// <summary>
        /// 初始化事件日志源
        /// </summary>
        /// <returns>异步任务</returns>
        public async Task InitializeAsync()
        {
            await Task.Run(() =>
            {
                try
                {
                    // 检查事件源是否存在，如果不存在则创建
                    if (!EventLog.SourceExists(EventSourceName))
                    {
                        EventLog.CreateEventSource(EventSourceName, EventLogName);
                        // 创建事件源后需要等待一段时间才能使用
                        System.Threading.Thread.Sleep(1000);
                    }
                    
                    _eventLogAvailable = true;
                    
                    // 记录初始化成功
                    LogToEventLog(EventLogEntryType.Information, EventIds.ServiceStarted, 
                        Properties.Resources.Log_EventLogger_InitSuccess);
                }
                catch (Exception ex)
                {
                    _eventLogAvailable = false;
                    
                    // 如果Windows事件日志不可用，记录到备用文件日志
                    LogToFile(EventLogEntryType.Warning, EventIds.ServiceException, 
                        string.Format(Properties.Resources.Log_EventLogger_InitFailed, ex.Message));
                }
            });
        }

        /// <summary>
        /// 记录信息级别日志
        /// </summary>
        /// <param name="eventId">事件ID</param>
        /// <param name="message">消息内容</param>
        /// <returns>异步任务</returns>
        public async Task LogInformationAsync(int eventId, string message)
        {
            await Task.Run(() =>
            {
                LogEntry(EventLogEntryType.Information, eventId, message, false);
            });
        }

        /// <summary>
        /// 记录警告级别日志
        /// </summary>
        /// <param name="eventId">事件ID</param>
        /// <param name="message">消息内容</param>
        /// <returns>异步任务</returns>
        public async Task LogWarningAsync(int eventId, string message)
        {
            await Task.Run(() =>
            {
                LogEntry(EventLogEntryType.Warning, eventId, message);
            });
        }

        /// <summary>
        /// 记录错误级别日志
        /// </summary>
        /// <param name="eventId">事件ID</param>
        /// <param name="message">消息内容</param>
        /// <param name="exception">异常信息（可选）</param>
        /// <returns>异步任务</returns>
        public async Task LogErrorAsync(int eventId, string message, Exception? exception = null)
        {
            await Task.Run(() =>
            {
                var fullMessage = exception != null
                    ? string.Format(Properties.Resources.Log_EventLogger_ExceptionDetails, message, exception)
                    : message;

                LogEntry(EventLogEntryType.Error, eventId, fullMessage);
            });
        }

        /// <summary>
        /// 记录日志条目的核心方法
        /// </summary>
        /// <param name="entryType">日志级别</param>
        /// <param name="eventId">事件ID</param>
        /// <param name="message">消息内容</param>
        /// <param name="withEventLog">是否写到EventLog</param>
        private void LogEntry(EventLogEntryType entryType, int eventId, string message, bool withEventLog=true)
        {
            lock (_lockObject)
            {
                try
                {
                    if (_eventLogAvailable && withEventLog)
                    {
                        LogToEventLog(entryType, eventId, message);
                    }
                    else
                    {
                        LogToFile(entryType, eventId, message);
                    }
                }
                catch (Exception ex)
                {
                    // 如果Windows事件日志写入失败，降级到文件日志
                    _eventLogAvailable = false;
                    LogToFile(entryType, eventId, string.Format(Properties.Resources.Log_EventLogger_EventLogWriteFailed, message, ex.Message));
                }
            }
        }

        /// <summary>
        /// 写入Windows事件日志
        /// </summary>
        /// <param name="entryType">日志级别</param>
        /// <param name="eventId">事件ID</param>
        /// <param name="message">消息内容</param>
        private void LogToEventLog(EventLogEntryType entryType, int eventId, string message)
        {
            using (var eventLog = new EventLog(EventLogName))
            {
                eventLog.Source = EventSourceName;
                eventLog.WriteEntry(message, entryType, eventId);
            }
        }

        /// <summary>
        /// 写入备用文件日志
        /// </summary>
        /// <param name="entryType">日志级别</param>
        /// <param name="eventId">事件ID</param>
        /// <param name="message">消息内容</param>
        private void LogToFile(EventLogEntryType entryType, int eventId, string message)
        {
            try
            {
                var timestamp = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
                var logLevel = entryType.ToString().ToUpper();
                var logEntry = $"[{timestamp}] [{logLevel}] [EventId:{eventId}] {message}{Environment.NewLine}";
                
                File.AppendAllText(_fallbackLogPath, logEntry);
            }
            catch (Exception ex)
            {
                // 如果连文件日志都写入失败，只能输出到调试控制台
                Debug.WriteLine(string.Format(Properties.Resources.Log_EventLogger_FileWriteFailed, eventId, message, ex.Message));
            }
        }

        /// <summary>
        /// 获取当前日志文件路径（用于调试和测试）
        /// </summary>
        /// <returns>日志文件路径</returns>
        public string GetLogFilePath()
        {
            return _fallbackLogPath;
        }

        /// <summary>
        /// 检查Windows事件日志是否可用
        /// </summary>
        /// <returns>是否可用</returns>
        public bool IsEventLogAvailable()
        {
            return _eventLogAvailable;
        }
    }
}