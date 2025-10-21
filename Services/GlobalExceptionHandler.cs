using System;
using System.Threading.Tasks;
using System.Windows.Forms;
using WindowsHelloIRHelper.Constants;
using WindowsHelloIRHelper.Interfaces;

namespace WindowsHelloIRHelper.Services
{
    /// <summary>
    /// 全局异常处理器
    /// 负责捕获和处理应用程序中的未处理异常
    /// </summary>
    public class GlobalExceptionHandler
    {
        private readonly IEventLogger _eventLogger;
        private readonly object _lockObject = new();
        private bool _isHandlingException = false;
        
        /// <summary>
        /// 构造函数
        /// </summary>
        /// <param name="eventLogger">事件日志记录器</param>
        public GlobalExceptionHandler(IEventLogger eventLogger)
        {
            _eventLogger = eventLogger ?? throw new ArgumentNullException(nameof(eventLogger));
        }
        
        /// <summary>
        /// 注册全局异常处理器
        /// </summary>
        public void RegisterGlobalHandlers()
        {
            // 注册应用程序域未处理异常事件
            AppDomain.CurrentDomain.UnhandledException += OnUnhandledException;
            
            // 注册任务调度器未观察到的任务异常事件
            TaskScheduler.UnobservedTaskException += OnUnobservedTaskException;
        }
        
        /// <summary>
        /// 注销全局异常处理器
        /// </summary>
        public void UnregisterGlobalHandlers()
        {
            AppDomain.CurrentDomain.UnhandledException -= OnUnhandledException;
            TaskScheduler.UnobservedTaskException -= OnUnobservedTaskException;
        }
        
        /// <summary>
        /// 处理应用程序域未处理异常
        /// </summary>
        /// <param name="sender">事件发送者</param>
        /// <param name="e">异常事件参数</param>
        private void OnUnhandledException(object sender, UnhandledExceptionEventArgs e)
        {
            try
            {
                lock (_lockObject)
                {
                    if (_isHandlingException)
                        return; // 防止递归异常处理
                    
                    _isHandlingException = true;
                }
                
                var exception = e.ExceptionObject as Exception;
                var isTerminating = e.IsTerminating;
                
                var message = isTerminating 
                    ? Properties.Resources.Log_Exception_UnhandledFatal
                    : Properties.Resources.Log_Exception_Unhandled;
                
                // 记录异常到事件日志 - 关键修复：减少超时时间，避免长时间阻塞
                try
                {
                    _eventLogger.LogErrorAsync(EventIds.ServiceException, message, exception).Wait(1000);
                }
                catch 
                { 
                    throw new Exception(message);  // 扔出异常让服务崩溃
                }
                
                // 如果程序即将终止，尝试进行清理工作
                if (isTerminating)
                {
                    HandleCriticalException(exception);
                }
            }
            catch (Exception handlerEx)
            {
                // 异常处理器本身发生异常，使用安全方法记录到Windows事件日志
                SafeLogToEventLog(string.Format(Properties.Resources.Log_Exception_HandlerFailed, handlerEx.Message),
                    System.Diagnostics.EventLogEntryType.Error);
                
                // 在关键异常情况下，让服务崩溃以避免不确定状态
                throw new Exception(Properties.Resources.Log_Exception_HandlerFailed.Split(':')[0]);
            }
            finally
            {
                lock (_lockObject)
                {
                    _isHandlingException = false;
                }
            }
        }
        
        /// <summary>
        /// 处理未观察到的任务异常
        /// </summary>
        /// <param name="sender">事件发送者</param>
        /// <param name="e">任务异常事件参数</param>
        private void OnUnobservedTaskException(object? sender, UnobservedTaskExceptionEventArgs e)
        {
            try
            {
                lock (_lockObject)
                {
                    if (_isHandlingException)
                        return; // 防止递归异常处理
                    
                    _isHandlingException = true;
                }
                
                // 记录任务异常 - 关键修复：减少超时时间
                try
                {
                    _eventLogger.LogErrorAsync(EventIds.ServiceException, 
                        Properties.Resources.Log_Exception_UnobservedTask, e.Exception).Wait(1000);
                }
                catch { }
                
                // 标记异常已被观察，防止程序崩溃
                e.SetObserved();
            }
            catch (Exception handlerEx)
            {
                // 异常处理器本身发生异常，使用安全方法记录到Windows事件日志
                SafeLogToEventLog(string.Format(Properties.Resources.Log_Exception_TaskHandlerFailed, handlerEx.Message),
                    System.Diagnostics.EventLogEntryType.Error);
            }
            finally
            {
                lock (_lockObject)
                {
                    _isHandlingException = false;
                }
            }
        }
        
        /// <summary>
        /// 处理关键异常
        /// </summary>
        /// <param name="exception">异常信息</param>
        private void HandleCriticalException(Exception? exception)
        {
            // 记录关键异常信息
            var message = string.Format(Properties.Resources.Log_Exception_ProgramTerminating, exception?.Message ?? "Unknown exception");
            
            // 使用安全方法写入Windows事件日志作为最后的记录
            SafeLogToEventLog(message, System.Diagnostics.EventLogEntryType.Error);
            
            // 可以在这里添加其他清理逻辑，如：
            // - 保存重要状态信息
            // - 通知其他组件进行清理
            // - 发送崩溃报告等
        }
        
        /// <summary>
        /// 安全地写入Windows事件日志，避免递归异常
        /// </summary>
        /// <param name="message">日志消息</param>
        /// <param name="entryType">日志类型</param>
        private void SafeLogToEventLog(string message, System.Diagnostics.EventLogEntryType entryType)
        {
            try
            {
                using var eventLog = new System.Diagnostics.EventLog("Application");
                eventLog.Source = "AutoCameraControlService";
                eventLog.WriteEntry(message, entryType);
            }
            catch
            {
                // 如果事件日志写入失败，尝试使用控制台输出作为最后手段
                try
                {
                    Console.WriteLine($"[Emergency Log] {entryType}: {message}");
                }
                catch
                {
                    // 如果连控制台都无法写入，则无法做更多处理
                }
            }
        }
        
        /// <summary>
        /// 安全执行操作，捕获并记录异常
        /// </summary>
        /// <param name="operation">要执行的操作</param>
        /// <param name="operationName">操作名称</param>
        /// <returns>操作是否成功</returns>
        public async Task<bool> SafeExecuteAsync(Func<Task> operation, string operationName)
        {
            try
            {
                await operation();
                return true;
            }
            catch (Exception ex)
            {
                await _eventLogger.LogErrorAsync(EventIds.ServiceException, 
                    string.Format(Properties.Resources.Log_Exception_OperationFailed, operationName), ex);
                return false;
            }
        }
        
        /// <summary>
        /// 安全执行操作，捕获并记录异常
        /// </summary>
        /// <typeparam name="T">返回值类型</typeparam>
        /// <param name="operation">要执行的操作</param>
        /// <param name="operationName">操作名称</param>
        /// <param name="defaultValue">异常时的默认返回值</param>
        /// <returns>操作结果或默认值</returns>
        public async Task<T> SafeExecuteAsync<T>(Func<Task<T>> operation, string operationName, T defaultValue)
        {
            try
            {
                return await operation();
            }
            catch (Exception ex)
            {
                await _eventLogger.LogErrorAsync(EventIds.ServiceException, 
                    string.Format(Properties.Resources.Log_Exception_OperationFailed, operationName), ex);
                return defaultValue;
            }
        }
        
        /// <summary>
        /// 安全执行同步操作，捕获并记录异常
        /// </summary>
        /// <param name="operation">要执行的操作</param>
        /// <param name="operationName">操作名称</param>
        /// <returns>操作是否成功</returns>
        public bool SafeExecute(Action operation, string operationName)
        {
            try
            {
                operation();
                return true;
            }
            catch (Exception ex)
            {
                // 关键修复：减少超时时间，避免长时间阻塞
                try
                {
                    _eventLogger.LogErrorAsync(EventIds.ServiceException, 
                        string.Format(Properties.Resources.Log_Exception_OperationFailed, operationName), ex).Wait(1000);
                }
                catch { }
                return false;
            }
        }
        
        /// <summary>
        /// 安全执行同步操作，捕获并记录异常
        /// </summary>
        /// <typeparam name="T">返回值类型</typeparam>
        /// <param name="operation">要执行的操作</param>
        /// <param name="operationName">操作名称</param>
        /// <param name="defaultValue">异常时的默认返回值</param>
        /// <returns>操作结果或默认值</returns>
        public T SafeExecute<T>(Func<T> operation, string operationName, T defaultValue)
        {
            try
            {
                return operation();
            }
            catch (Exception ex)
            {
                // 关键修复：减少超时时间，避免长时间阻塞
                try
                {
                    _eventLogger.LogErrorAsync(EventIds.ServiceException, 
                        string.Format(Properties.Resources.Log_Exception_OperationFailed, operationName), ex).Wait(1000);
                }
                catch { }
                return defaultValue;
            }
        }
    }
}