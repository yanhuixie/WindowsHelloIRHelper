namespace WindowsHelloIRHelper.Models
{
    /// <summary>
    /// 摄像头设备信息
    /// </summary>
    public class CameraDevice
    {
        /// <summary>
        /// 设备ID
        /// </summary>
        public string DeviceId { get; set; } = string.Empty;
        
        /// <summary>
        /// 设备名称
        /// </summary>
        public string Name { get; set; } = string.Empty;
        
        /// <summary>
        /// 设备描述
        /// </summary>
        public string Description { get; set; } = string.Empty;
        
        /// <summary>
        /// 当前状态
        /// </summary>
        // public DeviceState State { get; set; }

        public bool IsEnabled { get; set; }
        
        /// <summary>
        /// 是否为集成摄像头
        /// </summary>
        // public bool IsIntegrated { get; set; }
    }
}