using System;
using System.Security.Principal;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using WindowsHelloIRHelper.Interfaces;
using WindowsHelloIRHelper.Constants;

namespace WindowsHelloIRHelper.Utils
{
    /// <summary>
    /// 安全相关的辅助工具类
    /// </summary>
    public class SecurityHelper
    {
        private readonly IEventLogger _eventLogger;
        
        /// <summary>
        /// 构造函数
        /// </summary>
        /// <param name="eventLogger">事件日志记录器</param>
        public SecurityHelper(IEventLogger eventLogger)
        {
            _eventLogger = eventLogger ?? throw new ArgumentNullException(nameof(eventLogger));
        }
        /// <summary>
        /// 检查当前应用是否以管理员权限运行
        /// </summary>
        /// <returns>如果以管理员权限运行返回 true，否则返回 false</returns>
        public bool IsRunningAsAdministrator()
        {
            try
            {
                using (WindowsIdentity identity = WindowsIdentity.GetCurrent())
                {
                    WindowsPrincipal principal = new WindowsPrincipal(identity);
                    return principal.IsInRole(WindowsBuiltInRole.Administrator);
                }
            }
            catch
            {
                // 如果无法确定权限，默认返回 false
                return false;
            }
        }
        
        /// <summary>
        /// 验证设备实例 ID 格式是否有效（防止命令注入）
        /// </summary>
        /// <param name="deviceInstanceId">设备实例 ID</param>
        /// <returns>如果格式有效返回 true，否则返回 false</returns>
        public async Task<bool> IsValidDeviceInstanceIdAsync(string deviceInstanceId)
        {
            if (string.IsNullOrWhiteSpace(deviceInstanceId))
            {
                await _eventLogger.LogWarningAsync(EventIds.SecurityValidationFailed,
                    "DeviceID 验证失败: 为空或空白");
                return false;
            }

            // 设备实例 ID 通常包含字母、数字、反斜杠、下划线、连字符、&符号和#符号
            // 不应包含引号、分号、管道符等特殊字符
            var validPattern = @"^[A-Za-z0-9\\_\-&#]+$";
            bool isValid = Regex.IsMatch(deviceInstanceId, validPattern);
            
            if (!isValid)
            {
                await _eventLogger.LogWarningAsync(EventIds.SecurityValidationFailed,
                    $"DeviceID 验证失败: {SanitizeForLogging(deviceInstanceId)}");
                await _eventLogger.LogWarningAsync(EventIds.SecurityValidationFailed,
                    $"不匹配模式: {validPattern}");
            }
            else
            {
                await _eventLogger.LogInformationAsync(EventIds.SecurityValidationPassed,
                    $"DeviceID 验证通过: {SanitizeForLogging(deviceInstanceId)}");
            }
            
            return isValid;
        }
        
        /// <summary>
        /// 验证文件路径是否安全（防止路径遍历攻击）
        /// </summary>
        /// <param name="filePath">文件路径</param>
        /// <param name="basePath">基础路径</param>
        /// <returns>如果路径安全返回 true，否则返回 false</returns>
        public bool IsValidFilePath(string filePath, string basePath)
        {
            if (string.IsNullOrWhiteSpace(filePath) || string.IsNullOrWhiteSpace(basePath))
                return false;
            
            try
            {
                // 获取绝对路径
                string fullPath = Path.GetFullPath(filePath);
                string fullBasePath = Path.GetFullPath(basePath);
                
                // 检查文件路径是否在基础路径内
                return fullPath.StartsWith(fullBasePath, StringComparison.OrdinalIgnoreCase);
            }
            catch
            {
                return false;
            }
        }
        
        /// <summary>
        /// 清理敏感信息，用于日志记录
        /// </summary>
        /// <param name="message">原始消息</param>
        /// <returns>清理后的消息</returns>
        public string SanitizeForLogging(string message)
        {
            if (string.IsNullOrEmpty(message))
                return message;
            
            // 移除可能的敏感信息模式
            // 这里可以根据需要添加更多的清理规则
            var sanitized = message;
            
            // 移除可能的用户名模式
            sanitized = Regex.Replace(sanitized, @"\\[A-Za-z0-9]+\\", @"\[USER]\", RegexOptions.IgnoreCase);
            
            // 移除可能的路径信息中的用户名
            sanitized = Regex.Replace(sanitized, @"C:\\Users\\[^\\]+", @"C:\Users\[USER]", RegexOptions.IgnoreCase);
            
            return sanitized;
        }
    }
}