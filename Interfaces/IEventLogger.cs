using System;
using System.Threading.Tasks;

namespace WindowsHelloIRHelper.Interfaces
{
    /// <summary>
    /// 事件日志记录器接口
    /// </summary>
    public interface IEventLogger
    {
        /// <summary>
        /// 记录信息级别日志
        /// </summary>
        /// <param name="eventId">事件ID</param>
        /// <param name="message">消息内容</param>
        /// <returns>异步任务</returns>
        Task LogInformationAsync(int eventId, string message);
        
        /// <summary>
        /// 记录警告级别日志
        /// </summary>
        /// <param name="eventId">事件ID</param>
        /// <param name="message">消息内容</param>
        /// <returns>异步任务</returns>
        Task LogWarningAsync(int eventId, string message);
        
        /// <summary>
        /// 记录错误级别日志
        /// </summary>
        /// <param name="eventId">事件ID</param>
        /// <param name="message">消息内容</param>
        /// <param name="exception">异常信息（可选）</param>
        /// <returns>异步任务</returns>
        Task LogErrorAsync(int eventId, string message, Exception? exception = null);
        
        /// <summary>
        /// 初始化事件日志源
        /// </summary>
        /// <returns>异步任务</returns>
        Task InitializeAsync();
    }
}