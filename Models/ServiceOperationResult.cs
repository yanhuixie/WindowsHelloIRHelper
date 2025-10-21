namespace WindowsHelloIRHelper.Models
{
    /// <summary>
    /// 服务操作结果
    /// </summary>
    public class ServiceOperationResult
    {
        /// <summary>
        /// 操作是否成功
        /// </summary>
        public bool Success { get; set; }
        
        /// <summary>
        /// 操作结果消息
        /// </summary>
        public string Message { get; set; } = string.Empty;
        
        /// <summary>
        /// 错误详情（可选）
        /// </summary>
        public string? ErrorDetails { get; set; }
        
        /// <summary>
        /// 操作时间戳
        /// </summary>
        public DateTime Timestamp { get; set; } = DateTime.Now;
    }
}