namespace WindowsHelloIRHelper.Constants
{
    /// <summary>
    /// 事件日志ID常量
    /// </summary>
    public static class EventIds
    {
        // 信息级别事件 (1000-1999)
        /// <summary>
        /// 服务启动
        /// </summary>
        public const int ServiceStarted = 1001;
        
        /// <summary>
        /// 服务停止
        /// </summary>
        public const int ServiceStopped = 1002;
        
        /// <summary>
        /// 摄像头启用
        /// </summary>
        public const int CameraEnabled = 1003;
        
        /// <summary>
        /// 摄像头禁用
        /// </summary>
        public const int CameraDisabled = 1004;
        
        /// <summary>
        /// 配置重新加载
        /// </summary>
        public const int ConfigReloaded = 1005;
        
        /// <summary>
        /// 锁屏时因光亮度充足未禁用摄像头
        /// </summary>
        public const int LockScreenLightSufficient = 1006;
        
        /// <summary>
        /// 解锁时摄像头已启用无需操作
        /// </summary>
        public const int UnlockCameraAlreadyEnabled = 1007;
        
        /// <summary>
        /// 环境光传感器已启用
        /// </summary>
        public const int LightSensorEnabled = 1008;
        
        /// <summary>
        /// 环境光传感器已禁用
        /// </summary>
        public const int LightSensorDisabled = 1009;
        
        /// <summary>
        /// 摄像头控制触发
        /// </summary>
        public const int CameraControlTriggered = 1010;
        
        /// <summary>
        /// 摄像头已处于启用状态无需操作
        /// </summary>
        public const int CameraAlreadyEnabled = 1011;
        
        /// <summary>
        /// 摄像头已处于禁用状态无需操作
        /// </summary>
        public const int CameraAlreadyDisabled = 1012;
        
        /// <summary>
        /// 会话切换检测
        /// </summary>
        public const int SessionSwitchDetected = 1013;
        
        // 警告级别事件 (2000-2999)
        /// <summary>
        /// 配置文件无效
        /// </summary>
        public const int InvalidConfig = 2001;
        
        /// <summary>
        /// 传感器不可用
        /// </summary>
        public const int SensorUnavailable = 2002;
        
        /// <summary>
        /// 设备未找到
        /// </summary>
        public const int DeviceNotFound = 2003;
        
        /// <summary>
        /// 权限不足警告
        /// </summary>
        public const int InsufficientPermissions = 2004;
        
        /// <summary>
        /// 配置错误
        /// </summary>
        public const int ConfigurationError = 2005;
        
        // 错误级别事件 (3000-3999)
        /// <summary>
        /// 摄像头控制失败
        /// </summary>
        public const int CameraControlFailed = 3001;
        
        /// <summary>
        /// 服务异常
        /// </summary>
        public const int ServiceException = 3002;
        
        /// <summary>
        /// 配置加载失败
        /// </summary>
        public const int ConfigLoadFailed = 3003;
        
        /// <summary>
        /// 配置保存失败
        /// </summary>
        public const int ConfigSaveFailed = 3004;
        
        /// <summary>
        /// 监控器启动失败
        /// </summary>
        public const int MonitorStartFailed = 3005;
        
        /// <summary>
        /// 安全验证失败
        /// </summary>
        public const int SecurityValidationFailed = 3005;
        
        /// <summary>
        /// 安全验证通过
        /// </summary>
        public const int SecurityValidationPassed = 3006;
        
        /// <summary>
        /// 安全异常
        /// </summary>
        public const int SecurityException = 3007;
    }
}